using PrimaNota.Application.Abstractions;

namespace PrimaNota.Infrastructure.Identity;

/// <summary>
/// Fallback <see cref="ICurrentUserService"/> used until ASP.NET Core Identity is wired in
/// (TASK-006). Treats every request as anonymous so audit records capture that context.
/// </summary>
internal sealed class AnonymousCurrentUserService : ICurrentUserService
{
    /// <inheritdoc />
    public string? UserId => null;

    /// <inheritdoc />
    public string? UserName => null;

    /// <inheritdoc />
    public bool IsAuthenticated => false;
}
