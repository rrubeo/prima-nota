using PrimaNota.Application.PrimaNota.Import;

namespace PrimaNota.Application.Abstractions;

/// <summary>
/// Provider that retrieves electronic invoices (active/passive) from an external accredited
/// SdI channel (e.g. Aruba). Implementations only fetch the FatturaPA XML; parsing and movement
/// creation are reused from the existing import pipeline.
/// </summary>
public interface IFatturaProvider
{
    /// <summary>Gets a value indicating whether the provider is enabled and has credentials.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<bool> IsConfiguredAsync(CancellationToken cancellationToken = default);

    /// <summary>Lists the invoices available for a direction within a date range.</summary>
    /// <param name="direzione">Attiva (sent) or Passiva (received).</param>
    /// <param name="da">Range start (inclusive).</param>
    /// <param name="a">Range end (inclusive).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The invoice metadata found in the range.</returns>
    Task<IReadOnlyList<FatturaRemota>> ListAsync(
        DirezioneFattura direzione,
        DateOnly da,
        DateOnly a,
        CancellationToken cancellationToken = default);

    /// <summary>Downloads a single invoice and returns its decoded FatturaPA XML.</summary>
    /// <param name="direzione">Attiva (sent) or Passiva (received).</param>
    /// <param name="id">Provider-specific invoice id (from <see cref="FatturaRemota.Id"/>).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The downloaded invoice (SdI id + XML bytes, p7m already unwrapped).</returns>
    Task<FatturaScaricata> DownloadAsync(
        DirezioneFattura direzione,
        string id,
        CancellationToken cancellationToken = default);
}

/// <summary>Metadata of a remote invoice listed by a provider.</summary>
/// <param name="Id">Provider-specific id used to download the invoice.</param>
/// <param name="IdentificativoSdi">Stable SdI identifier (used for dedup).</param>
/// <param name="Numero">Invoice number (display).</param>
/// <param name="Data">Invoice date.</param>
/// <param name="Controparte">Counterparty display name.</param>
/// <param name="ContropartePartitaIva">Counterparty VAT code.</param>
public sealed record FatturaRemota(
    string Id,
    string IdentificativoSdi,
    string? Numero,
    DateOnly Data,
    string? Controparte,
    string? ContropartePartitaIva);

/// <summary>A downloaded invoice ready to be imported.</summary>
/// <param name="IdentificativoSdi">Stable SdI identifier (dedup key).</param>
/// <param name="Xml">Decoded FatturaPA XML bytes (signature envelope removed).</param>
public sealed record FatturaScaricata(string IdentificativoSdi, byte[] Xml);
