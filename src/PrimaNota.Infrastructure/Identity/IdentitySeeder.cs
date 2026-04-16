using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PrimaNota.Shared.Authorization;

namespace PrimaNota.Infrastructure.Identity;

/// <summary>
/// Ensures the canonical roles (Admin, Contabile, Dipendente) exist and, if configured,
/// provisions the initial admin user. Safe to call on every startup: operations are idempotent.
/// </summary>
public sealed class IdentitySeeder
{
    private readonly RoleManager<ApplicationRole> roleManager;
    private readonly UserManager<ApplicationUser> userManager;
    private readonly IdentityBootstrapOptions bootstrap;
    private readonly ILogger<IdentitySeeder> logger;

    /// <summary>Initializes a new instance of the <see cref="IdentitySeeder"/> class.</summary>
    /// <param name="roleManager">Identity role manager.</param>
    /// <param name="userManager">Identity user manager.</param>
    /// <param name="bootstrap">Optional bootstrap admin configuration.</param>
    /// <param name="logger">Logger.</param>
    public IdentitySeeder(
        RoleManager<ApplicationRole> roleManager,
        UserManager<ApplicationUser> userManager,
        IOptions<IdentityBootstrapOptions> bootstrap,
        ILogger<IdentitySeeder> logger)
    {
        this.roleManager = roleManager;
        this.userManager = userManager;
        this.bootstrap = bootstrap.Value;
        this.logger = logger;
    }

    /// <summary>Runs the idempotent seed sequence.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when seeding is done.</returns>
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await SeedRolesAsync();
        await SeedAdminAsync(cancellationToken);
    }

    private static string DescribeRole(string roleName) => roleName switch
    {
        UserRoles.Admin => "Amministratore di sistema: gestione utenti, configurazione, chiusure anno.",
        UserRoles.Contabile => "Contabile/Amministrazione: prima nota, riconciliazioni, IVA, report.",
        UserRoles.Dipendente => "Dipendente: inserimento e consultazione delle proprie note spese.",
        _ => string.Empty,
    };

    private async Task SeedRolesAsync()
    {
        foreach (var roleName in UserRoles.All)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                var role = new ApplicationRole(roleName)
                {
                    Description = DescribeRole(roleName),
                };
                var result = await roleManager.CreateAsync(role);
                if (!result.Succeeded)
                {
                    logger.LogError(
                        "Failed to create role {RoleName}: {Errors}",
                        roleName,
                        string.Join(", ", result.Errors.Select(e => e.Description)));
                }
                else
                {
                    logger.LogInformation("Seeded role {RoleName}", roleName);
                }
            }
        }
    }

    private async Task SeedAdminAsync(CancellationToken cancellationToken)
    {
        if (!bootstrap.IsEnabled)
        {
            return;
        }

        cancellationToken.ThrowIfCancellationRequested();

        var existing = await userManager.FindByEmailAsync(bootstrap.Email!);
        if (existing is not null)
        {
            return;
        }

        var admin = new ApplicationUser
        {
            UserName = bootstrap.Email,
            Email = bootstrap.Email,
            EmailConfirmed = true,
            FullName = bootstrap.FullName!,
            IsActive = true,
        };

        var create = await userManager.CreateAsync(admin, bootstrap.Password!);
        if (!create.Succeeded)
        {
            logger.LogError(
                "Failed to bootstrap admin user {Email}: {Errors}",
                bootstrap.Email,
                string.Join(", ", create.Errors.Select(e => e.Description)));
            return;
        }

        var addRole = await userManager.AddToRoleAsync(admin, UserRoles.Admin);
        if (!addRole.Succeeded)
        {
            logger.LogError(
                "Failed to assign Admin role to bootstrap user {Email}: {Errors}",
                bootstrap.Email,
                string.Join(", ", addRole.Errors.Select(e => e.Description)));
            return;
        }

        logger.LogWarning(
            "Bootstrap admin user provisioned: {Email}. Remove the Identity:Bootstrap configuration after first login.",
            bootstrap.Email);
    }
}
