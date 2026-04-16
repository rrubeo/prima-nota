using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using PrimaNota.Application.Abstractions;
using PrimaNota.Domain.Audit;
using PrimaNota.Infrastructure.Configuration;
using PrimaNota.Infrastructure.Identity;
using PrimaNota.Shared.Authorization;

namespace PrimaNota.Web.Authentication;

/// <summary>Minimal-API endpoints that drive the login/logout/external-auth flow.</summary>
internal static class AccountEndpoints
{
    public static IEndpointRouteBuilder MapAccountEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/Account").DisableAntiforgery();

        group.MapPost("/Login", HandleLoginAsync);
        group.MapPost("/Logout", HandleLogoutAsync);
        group.MapPost("/ChangePassword", HandleChangePasswordAsync).RequireAuthorization();
        group.MapGet("/ExternalLogin", HandleExternalChallenge);
        group.MapGet("/ExternalCallback", HandleExternalCallbackAsync);

        return app;
    }

    private static async Task<IResult> HandleChangePasswordAsync(
        [FromForm] string currentPassword,
        [FromForm] string newPassword,
        [FromForm] string confirmPassword,
        [FromForm] string? returnUrl,
        HttpContext http,
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        IAuditLogger audit)
    {
        var redirectTarget = SanitizeReturnUrl(returnUrl);
        var failBase = $"/account/cambia-password?returnUrl={Uri.EscapeDataString(redirectTarget)}";

        if (string.IsNullOrWhiteSpace(currentPassword) ||
            string.IsNullOrWhiteSpace(newPassword) ||
            string.IsNullOrWhiteSpace(confirmPassword))
        {
            return Results.Redirect(failBase + "&error=missing");
        }

        if (!string.Equals(newPassword, confirmPassword, StringComparison.Ordinal))
        {
            return Results.Redirect(failBase + "&error=mismatch");
        }

        var user = await userManager.GetUserAsync(http.User);
        if (user is null)
        {
            return Results.Redirect("/Account/Login");
        }

        var result = await userManager.ChangePasswordAsync(user, currentPassword, newPassword);
        if (!result.Succeeded)
        {
            var first = result.Errors.FirstOrDefault()?.Code ?? "invalid";
            var reason = first switch
            {
                "PasswordMismatch" => "wrong_current",
                "PasswordTooShort" or "PasswordRequiresDigit" or "PasswordRequiresLower"
                    or "PasswordRequiresUpper" or "PasswordRequiresNonAlphanumeric" => "weak",
                _ => "invalid",
            };
            await audit.LogAsync(
                AuditEventKind.PasswordChanged,
                $"Password change failed for {user.Email}: {first}",
                targetType: "ApplicationUser",
                targetId: user.Id);
            return Results.Redirect(failBase + $"&error={reason}");
        }

        await signInManager.RefreshSignInAsync(user);
        await audit.LogAsync(
            AuditEventKind.PasswordChanged,
            $"Password changed for {user.Email}",
            targetType: "ApplicationUser",
            targetId: user.Id);

        return Results.Redirect(redirectTarget + (redirectTarget.Contains('?') ? "&" : "?") + "passwordChanged=1");
    }

    private static async Task<IResult> HandleLoginAsync(
        [FromForm] string email,
        [FromForm] string password,
        [FromForm] bool? rememberMe,
        [FromForm] string? returnUrl,
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        IAuditLogger audit)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            return Results.Redirect(BuildLoginRedirect(returnUrl, "missing"));
        }

        var user = await userManager.FindByEmailAsync(email);
        if (user is null || !user.IsActive)
        {
            await audit.LogAsync(AuditEventKind.LoginFailed, $"Login failed for {email}: unknown or inactive user");
            return Results.Redirect(BuildLoginRedirect(returnUrl, "invalid"));
        }

        var result = await signInManager.PasswordSignInAsync(
            user,
            password,
            isPersistent: rememberMe ?? false,
            lockoutOnFailure: true);

        if (!result.Succeeded)
        {
            var reason = ResolveFailureReason(result);
            await audit.LogAsync(
                AuditEventKind.LoginFailed,
                $"Login failed for {email}: {reason}",
                targetType: "ApplicationUser",
                targetId: user.Id);
            return Results.Redirect(BuildLoginRedirect(returnUrl, reason));
        }

        user.LastLoginAt = DateTimeOffset.UtcNow;
        await userManager.UpdateAsync(user);

        await audit.LogAsync(
            AuditEventKind.LoginSucceeded,
            $"{user.Email} signed in",
            targetType: "ApplicationUser",
            targetId: user.Id);

        return Results.LocalRedirect(SanitizeReturnUrl(returnUrl));
    }

    private static async Task<IResult> HandleLogoutAsync(
        SignInManager<ApplicationUser> signInManager,
        IAuditLogger audit)
    {
        await audit.LogAsync(AuditEventKind.Logout, "User signed out");
        await signInManager.SignOutAsync();
        return Results.LocalRedirect("/");
    }

    private static IResult HandleExternalChallenge(
        [FromQuery] string provider,
        [FromQuery] string? returnUrl,
        IOptions<GoogleAuthenticationOptions> googleOptions)
    {
        if (!string.Equals(provider, GoogleDefaults.AuthenticationScheme, StringComparison.OrdinalIgnoreCase))
        {
            return Results.BadRequest(new { error = "UNSUPPORTED_PROVIDER" });
        }

        if (!googleOptions.Value.IsConfigured)
        {
            return Results.BadRequest(new { error = "GOOGLE_NOT_CONFIGURED" });
        }

        var callback = $"/Account/ExternalCallback?returnUrl={Uri.EscapeDataString(SanitizeReturnUrl(returnUrl))}";
        var props = new AuthenticationProperties { RedirectUri = callback };
        return Results.Challenge(props, [GoogleDefaults.AuthenticationScheme]);
    }

    private static async Task<IResult> HandleExternalCallbackAsync(
        [FromQuery] string? returnUrl,
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        IAuditLogger audit)
    {
        var info = await signInManager.GetExternalLoginInfoAsync();
        if (info is null)
        {
            await audit.LogAsync(AuditEventKind.ExternalLoginFailed, "External provider returned no login info");
            return Results.Redirect(BuildLoginRedirect(returnUrl, "external_failed"));
        }

        var signIn = await signInManager.ExternalLoginSignInAsync(
            info.LoginProvider,
            info.ProviderKey,
            isPersistent: false,
            bypassTwoFactor: true);

        if (signIn.Succeeded)
        {
            await audit.LogAsync(
                AuditEventKind.ExternalLoginSucceeded,
                $"External sign-in via {info.LoginProvider}",
                targetType: "ApplicationUser",
                targetId: info.ProviderKey);
            return Results.LocalRedirect(SanitizeReturnUrl(returnUrl));
        }

        var email = info.Principal.FindFirstValue(ClaimTypes.Email);
        if (string.IsNullOrWhiteSpace(email))
        {
            return Results.Redirect(BuildLoginRedirect(returnUrl, "external_no_email"));
        }

        var existing = await userManager.FindByEmailAsync(email);
        if (existing is null || !existing.IsActive)
        {
            // Policy: Google sign-in is allowed only for pre-provisioned active users.
            // Self-registration from external providers is disabled.
            return Results.Redirect(BuildLoginRedirect(returnUrl, "external_not_provisioned"));
        }

        var addLogin = await userManager.AddLoginAsync(existing, info);
        if (!addLogin.Succeeded)
        {
            return Results.Redirect(BuildLoginRedirect(returnUrl, "external_link_failed"));
        }

        await signInManager.SignInAsync(existing, isPersistent: false);
        existing.LastLoginAt = DateTimeOffset.UtcNow;
        await userManager.UpdateAsync(existing);

        return Results.LocalRedirect(SanitizeReturnUrl(returnUrl));
    }

    private static string ResolveFailureReason(Microsoft.AspNetCore.Identity.SignInResult result)
    {
        if (result.IsLockedOut)
        {
            return "lockout";
        }

        if (result.IsNotAllowed)
        {
            return "notallowed";
        }

        return "invalid";
    }

    private static string BuildLoginRedirect(string? returnUrl, string error)
    {
        var safe = SanitizeReturnUrl(returnUrl);
        return $"/Account/Login?error={error}&returnUrl={Uri.EscapeDataString(safe)}";
    }

    private static string SanitizeReturnUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return "/";
        }

        // Only allow relative URLs within the same origin to prevent open redirect.
        return url.StartsWith('/') && !url.StartsWith("//", StringComparison.Ordinal) ? url : "/";
    }
}
