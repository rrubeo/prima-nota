using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using PrimaNota.Application.Abstractions;

namespace PrimaNota.Infrastructure.Identity;

/// <summary>
/// Resolves the current user identity from the ambient <see cref="HttpContext"/>.
/// Returns anonymous values when called outside a request (e.g. from a background job).
/// </summary>
internal sealed class HttpContextCurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor accessor;

    public HttpContextCurrentUserService(IHttpContextAccessor accessor)
    {
        this.accessor = accessor;
    }

    /// <inheritdoc />
    public string? UserId => Principal?.FindFirstValue(ClaimTypes.NameIdentifier);

    /// <inheritdoc />
    public string? UserName => Principal?.Identity?.Name;

    /// <inheritdoc />
    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;

    private ClaimsPrincipal? Principal => accessor.HttpContext?.User;
}
