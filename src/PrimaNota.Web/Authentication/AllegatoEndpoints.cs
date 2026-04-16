using MediatR;
using Microsoft.AspNetCore.Mvc;
using PrimaNota.Application.PrimaNota;
using PrimaNota.Shared.Authorization;

namespace PrimaNota.Web.Authentication;

/// <summary>Minimal-API endpoints for attachment download and upload.</summary>
internal static class AllegatoEndpoints
{
    public static IEndpointRouteBuilder MapAllegatoEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/allegati").RequireAuthorization(AuthorizationPolicies.RequireAuthenticated);

        group.MapGet("/{allegatoId:guid}", HandleDownloadAsync);
        group.MapPost("/{movimentoId:guid}/upload", HandleUploadAsync)
            .DisableAntiforgery();

        return app;
    }

    private static async Task<IResult> HandleDownloadAsync(
        Guid allegatoId,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var dl = await mediator.Send(new GetAllegatoContent(allegatoId), cancellationToken);
        if (dl is null)
        {
            return Results.NotFound();
        }

        return Results.File(dl.Content, dl.MimeType, dl.FileName);
    }

    private static async Task<IResult> HandleUploadAsync(
        Guid movimentoId,
        [FromForm] IFormFile file,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return Results.BadRequest(new { error = "MISSING_FILE" });
        }

        await using var stream = file.OpenReadStream();
        var id = await mediator.Send(
            new UploadAllegato(movimentoId, file.FileName, file.ContentType, stream),
            cancellationToken);

        return Results.Ok(new { id });
    }
}
