using Hangfire.Dashboard;
using PrimaNota.Shared.Authorization;

namespace PrimaNota.Web.BackgroundJobs;

/// <summary>
/// Allows access to the Hangfire dashboard only to authenticated users in the Admin role.
/// </summary>
internal sealed class HangfireAdminAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var user = context.GetHttpContext().User;
        return user.Identity?.IsAuthenticated == true && user.IsInRole(UserRoles.Admin);
    }
}
