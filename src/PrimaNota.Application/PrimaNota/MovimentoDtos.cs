using PrimaNota.Domain.PrimaNota;

namespace PrimaNota.Application.PrimaNota;

/// <summary>Compact projection for the movements list page.</summary>
public sealed record MovimentoListItemDto(
    Guid Id,
    DateOnly Data,
    string Descrizione,
    string? Numero,
    Guid CausaleId,
    string CausaleCodice,
    string CausaleNome,
    string? AnagraficaRagioneSociale,
    decimal Totale,
    int NumeroRighe,
    StatoMovimento Stato,
    int AllegatiCount,
    decimal Residuo,
    bool IsFullyPaid);

/// <summary>Detail projection for the edit page.</summary>
public sealed record MovimentoDto(
    Guid Id,
    DateOnly Data,
    DateOnly DataCompetenza,
    int EsercizioAnno,
    string Descrizione,
    string? Numero,
    Guid CausaleId,
    Guid? AnagraficaId,
    StatoMovimento Stato,
    string? Note,
    byte[] RowVersion,
    IReadOnlyList<RigaMovimentoDto> Righe,
    IReadOnlyList<AllegatoDto> Allegati,
    IReadOnlyList<PagamentoMovimentoDto> Pagamenti,
    decimal Totale,
    decimal TotalePagato,
    decimal Residuo,
    bool IsFullyPaid,
    DateOnly? DataPagamento);

/// <summary>Line projection.</summary>
public sealed record RigaMovimentoDto(
    Guid Id,
    decimal Importo,
    Guid ContoFinanziarioId,
    Guid CategoriaId,
    Guid? AnagraficaId,
    Guid? AliquotaIvaId,
    string? Note);

/// <summary>Attachment projection.</summary>
public sealed record AllegatoDto(
    Guid Id,
    string NomeFile,
    string MimeType,
    long Size,
    DateTimeOffset UploadedAt);

/// <summary>Payment projection.</summary>
/// <param name="Id">Payment id.</param>
/// <param name="Data">Value date.</param>
/// <param name="Importo">Positive settlement amount.</param>
/// <param name="ContoFinanziarioId">Financial account.</param>
/// <param name="ContoFinanziarioNome">Financial account display name.</param>
/// <param name="Note">Optional note.</param>
public sealed record PagamentoMovimentoDto(
    Guid Id,
    DateOnly Data,
    decimal Importo,
    Guid ContoFinanziarioId,
    string? ContoFinanziarioNome,
    string? Note);
