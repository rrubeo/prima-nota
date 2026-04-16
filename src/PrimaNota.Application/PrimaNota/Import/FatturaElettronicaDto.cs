namespace PrimaNota.Application.PrimaNota.Import;

/// <summary>
/// Parsed shape of an Italian electronic invoice (FatturaElettronica FPR12/FPA12).
/// Only the fields needed to produce a prima-nota movement are extracted.
/// </summary>
public sealed record FatturaElettronicaDto(
    FatturaSoggettoDto Cedente,
    FatturaSoggettoDto Cessionario,
    DateOnly Data,
    string? Numero,
    decimal ImportoTotale,
    string Divisa,
    IReadOnlyList<FatturaRiepilogoDto> Riepilogo);

/// <summary>One of the two parties on the invoice (cedente or cessionario).</summary>
/// <param name="Denominazione">Legal name for legal entities.</param>
/// <param name="Nome">First name for natural persons.</param>
/// <param name="Cognome">Last name for natural persons.</param>
/// <param name="PartitaIva">VAT number, stripped of country prefix.</param>
/// <param name="CodiceFiscale">Italian fiscal code.</param>
/// <param name="PaeseIva">ISO country code of the VAT number.</param>
/// <param name="Indirizzo">Street address.</param>
/// <param name="Cap">Postal code.</param>
/// <param name="Comune">City.</param>
/// <param name="Provincia">Province (IT only).</param>
/// <param name="Nazione">ISO country code.</param>
/// <param name="Email">Primary email when available.</param>
public sealed record FatturaSoggettoDto(
    string? Denominazione,
    string? Nome,
    string? Cognome,
    string? PartitaIva,
    string? CodiceFiscale,
    string? PaeseIva,
    string? Indirizzo,
    string? Cap,
    string? Comune,
    string? Provincia,
    string? Nazione,
    string? Email)
{
    /// <summary>Gets the best display name for this party.</summary>
    public string DisplayName =>
        !string.IsNullOrWhiteSpace(Denominazione)
            ? Denominazione
            : string.Join(' ', new[] { Nome, Cognome }.Where(s => !string.IsNullOrWhiteSpace(s)));
}

/// <summary>Tax summary row (one per VAT rate + Natura combination).</summary>
/// <param name="AliquotaPercentuale">VAT percentage (0 for special regimes).</param>
/// <param name="Natura">Nature code (N1..N7) for non-ordinary regimes; null for ordinary rates.</param>
/// <param name="Imponibile">Taxable base.</param>
/// <param name="Imposta">VAT amount.</param>
/// <param name="RiferimentoNormativo">Free-text legal reference (if any).</param>
public sealed record FatturaRiepilogoDto(
    decimal AliquotaPercentuale,
    string? Natura,
    decimal Imponibile,
    decimal Imposta,
    string? RiferimentoNormativo)
{
    /// <summary>Gets the gross amount for this summary row.</summary>
    public decimal Totale => Imponibile + Imposta;
}
