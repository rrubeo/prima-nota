namespace PrimaNota.Application.Anagrafiche;

/// <summary>Full anagrafica projection used by the edit form and detail view.</summary>
public sealed record AnagraficaDto(
    Guid Id,
    string RagioneSociale,
    string? Nome,
    string? Cognome,
    string? CodiceFiscale,
    string? PartitaIva,
    bool PersonaFisica,
    bool IsCliente,
    bool IsFornitore,
    bool IsDipendente,
    string? Mansione,
    DateOnly? DataAssunzione,
    DateOnly? DataCessazione,
    string? Email,
    string? Telefono,
    string? Pec,
    string? IndirizzoVia,
    string? IndirizzoCap,
    string? IndirizzoCitta,
    string? IndirizzoProvincia,
    string IndirizzoCountryCode,
    bool Attivo,
    string? Note,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

/// <summary>Compact projection used by the list page.</summary>
public sealed record AnagraficaListItemDto(
    Guid Id,
    string RagioneSociale,
    string? CodiceFiscale,
    string? PartitaIva,
    bool IsCliente,
    bool IsFornitore,
    bool IsDipendente,
    string? Email,
    string? Telefono,
    bool Attivo);
