using PrimaNota.Domain.PrimaNota;

namespace PrimaNota.Application.PrimaNota;

/// <summary>Compact projection for the movements list page.</summary>
public sealed record MovimentoListItemDto(
    Guid Id,
    DateOnly Data,
    string Descrizione,
    string? Numero,
    string CausaleCodice,
    string CausaleNome,
    string? AnagraficaRagioneSociale,
    decimal Totale,
    int NumeroRighe,
    StatoMovimento Stato,
    int AllegatiCount);

/// <summary>Detail projection for the edit page.</summary>
public sealed record MovimentoDto(
    Guid Id,
    DateOnly Data,
    int EsercizioAnno,
    string Descrizione,
    string? Numero,
    Guid CausaleId,
    Guid? AnagraficaId,
    StatoMovimento Stato,
    string? Note,
    byte[] RowVersion,
    IReadOnlyList<RigaMovimentoDto> Righe,
    IReadOnlyList<AllegatoDto> Allegati);

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
