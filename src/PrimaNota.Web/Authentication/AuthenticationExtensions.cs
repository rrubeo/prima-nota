using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using PrimaNota.Infrastructure.Configuration;
using PrimaNota.Infrastructure.Identity;
using PrimaNota.Shared.Authorization;

namespace PrimaNota.Web.Authentication;

/// <summary>Wires authentication (cookie + Google) and authorization policies on the Web host.</summary>
internal static class AuthenticationExtensions
{
    public static IServiceCollection AddPrimaNotaAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<GoogleAuthenticationOptions>()
            .Bind(configuration.GetSection(GoogleAuthenticationOptions.SectionName));

        services.ConfigureApplicationCookie(options =>
        {
            options.Cookie.Name = "PrimaNota.Auth";
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.ExpireTimeSpan = TimeSpan.FromHours(8);
            options.SlidingExpiration = true;
            options.LoginPath = "/Account/Login";
            options.LogoutPath = "/Account/Logout";
            options.AccessDeniedPath = "/Account/AccessDenied";
        });

        var authBuilder = services.AddAuthentication(o =>
        {
            o.DefaultScheme = IdentityConstants.ApplicationScheme;
            o.DefaultSignInScheme = IdentityConstants.ExternalScheme;
            o.DefaultChallengeScheme = IdentityConstants.ApplicationScheme;
        })
        .AddCookie(IdentityConstants.ApplicationScheme)
        .AddCookie(IdentityConstants.ExternalScheme, o =>
        {
            o.Cookie.Name = "PrimaNota.External";
            o.ExpireTimeSpan = TimeSpan.FromMinutes(5);
        });

        var googleOptions = configuration
            .GetSection(GoogleAuthenticationOptions.SectionName)
            .Get<GoogleAuthenticationOptions>();

        if (googleOptions?.IsConfigured == true)
        {
            authBuilder.AddGoogle(GoogleDefaults.AuthenticationScheme, o =>
            {
                o.ClientId = googleOptions.ClientId!;
                o.ClientSecret = googleOptions.ClientSecret!;
                o.SignInScheme = IdentityConstants.ExternalScheme;
                o.CallbackPath = "/signin-google";
                o.SaveTokens = false;
            });
        }

        services.AddAuthorization(options =>
        {
            options.AddPolicy(AuthorizationPolicies.RequireAdmin, p => p
                .RequireAuthenticatedUser()
                .RequireRole(UserRoles.Admin));

            options.AddPolicy(AuthorizationPolicies.RequireContabile, p => p
                .RequireAuthenticatedUser()
                .RequireRole(UserRoles.Admin, UserRoles.Contabile));

            options.AddPolicy(AuthorizationPolicies.RequireAuthenticated, p => p
                .RequireAuthenticatedUser());
        });

        services.AddCascadingAuthenticationState();

        return services;
    }
}
