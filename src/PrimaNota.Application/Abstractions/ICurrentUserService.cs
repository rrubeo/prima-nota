namespace PrimaNota.Application.Abstractions;

/// <summary>
/// Exposes the currently authenticated user identity to the application layer
/// without depending on ASP.NET Core types.
/// </summary>
public interface ICurrentUserService
{
    /// <summary>
    /// Gets the stable unique identifier of the current user (typically the Identity <c>UserId</c>),
    /// or <c>null</c> when no user is authenticated (anonymous requests, background jobs).
    /// </summary>
    string? UserId { get; }

    /// <summary>Gets the display name of the current user, or <c>null</c> if unauthenticated.</summary>
    string? UserName { get; }

    /// <summary>Gets a value indicating whether a user is currently authenticated.</summary>
    bool IsAuthenticated { get; }
}
