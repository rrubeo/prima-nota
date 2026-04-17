using MediatR;
using PrimaNota.Application.Iva;
using PrimaNota.Application.Reporting;
using PrimaNota.Domain.Iva;
using PrimaNota.Shared.Authorization;

namespace PrimaNota.Web.Endpoints;

/// <summary>Minimal-API endpoints for report downloads.</summary>
internal static class ExportEndpoints
{
    /// <summary>Maps report export endpoints.</summary>
    /// <param name="app">Endpoint route builder.</param>
    /// <returns>The same builder for chaining.</returns>
    public static IEndpointRouteBuilder MapExportEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/export").RequireAuthorization(AuthorizationPolicies.RequireContabile);

        group.MapGet("/movimenti/{anno:int}", HandleMovimentiAsync);
        group.MapGet("/registro-iva/{registro}/{anno:int}/{periodicita}/{indice:int}", HandleRegistroAsync);
        group.MapGet("/liquidazione/{anno:int}/{periodicita}/{indice:int}", HandleLiquidazioneAsync);
        group.MapGet("/scheda/{anagraficaId:guid}/{anno:int?}", HandleSchedaAsync);
        group.MapGet("/commercialista/{anno:int}/{periodicita}", HandlePacchettoAsync);

        return app;
    }

    private static async Task<IResult> HandleMovimentiAsync(int anno, IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(new ExportMovimentiExcel(anno), ct);
        return Results.File(result.Content, result.ContentType, result.FileName);
    }

    private static async Task<IResult> HandleRegistroAsync(
        TipoRegistroIva registro,
        int anno,
        PeriodicitaIva periodicita,
        int indice,
        IMediator mediator,
        CancellationToken ct)
    {
        var periodo = new IvaPeriodo(anno, periodicita, indice);
        var result = await mediator.Send(new ExportRegistroIvaExcel(registro, periodo), ct);
        return Results.File(result.Content, result.ContentType, result.FileName);
    }

    private static async Task<IResult> HandleLiquidazioneAsync(
        int anno,
        PeriodicitaIva periodicita,
        int indice,
        IMediator mediator,
        CancellationToken ct)
    {
        var periodo = new IvaPeriodo(anno, periodicita, indice);
        var result = await mediator.Send(new ExportLiquidazioneExcel(periodo), ct);
        return Results.File(result.Content, result.ContentType, result.FileName);
    }

    private static async Task<IResult> HandleSchedaAsync(
        Guid anagraficaId,
        int? anno,
        IMediator mediator,
        CancellationToken ct)
    {
        var result = await mediator.Send(new ExportSchedaAnagraficaExcel(anagraficaId, anno), ct);
        return Results.File(result.Content, result.ContentType, result.FileName);
    }

    private static async Task<IResult> HandlePacchettoAsync(
        int anno,
        PeriodicitaIva periodicita,
        IMediator mediator,
        CancellationToken ct)
    {
        var result = await mediator.Send(new ExportPacchettoCommercialista(anno, periodicita), ct);
        return Results.File(result.Content, result.ContentType, result.FileName);
    }
}
