using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PrimaNota.Application.Abstractions;
using PrimaNota.Domain.Audit;
using PrimaNota.Infrastructure.Persistence;
using PrimaNota.Shared.Clock;

namespace PrimaNota.Infrastructure.Audit;

/// <summary>Default <see cref="IAuditLogger"/> that persists entries via EF Core.</summary>
internal sealed class AuditLogger : IAuditLogger
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false,
    };

    private readonly AppDbContext db;
    private readonly ICurrentUserService currentUser;
    private readonly IDateTimeProvider clock;
    private readonly IHttpContextAccessor httpContext;
    private readonly ILogger<AuditLogger> logger;

    public AuditLogger(
        AppDbContext db,
        ICurrentUserService currentUser,
        IDateTimeProvider clock,
        IHttpContextAccessor httpContext,
        ILogger<AuditLogger> logger)
    {
        this.db = db;
        this.currentUser = currentUser;
        this.clock = clock;
        this.httpContext = httpContext;
        this.logger = logger;
    }

    /// <inheritdoc />
    public async Task LogAsync(
        AuditEventKind kind,
        string summary,
        string targetType = "",
        string targetId = "",
        object? payload = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var payloadJson = payload is null ? null : JsonSerializer.Serialize(payload, JsonOptions);
            var correlationId = httpContext.HttpContext?.TraceIdentifier;
            var ip = httpContext.HttpContext?.Connection.RemoteIpAddress?.ToString();

            var entry = new AuditLogEntry(
                clock.UtcNow,
                kind,
                currentUser.UserId,
                currentUser.UserName,
                targetType,
                targetId,
                summary,
                payloadJson,
                correlationId,
                ip);

            db.Set<AuditLogEntry>().Add(entry);
            await db.SaveChangesAsync(cancellationToken);
        }
#pragma warning disable CA1031 // Audit logging must never break the caller flow
        catch (Exception ex)
#pragma warning restore CA1031
        {
            logger.LogError(ex, "Failed to write audit log entry for {Kind}: {Summary}", kind, summary);
        }
    }
}
