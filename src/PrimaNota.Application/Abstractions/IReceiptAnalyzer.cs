namespace PrimaNota.Application.Abstractions;

/// <summary>Analyzes a receipt image and extracts structured expense data.</summary>
public interface IReceiptAnalyzer
{
    /// <summary>Analyzes the given image.</summary>
    /// <param name="imageBytes">Raw image bytes (JPEG/PNG).</param>
    /// <param name="mimeType">MIME type of the image.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Extracted receipt data.</returns>
    Task<ReceiptAnalysisResult> AnalyzeAsync(byte[] imageBytes, string mimeType, CancellationToken cancellationToken = default);
}

/// <summary>Structured data extracted from a receipt image.</summary>
/// <param name="Data">Date on the receipt (null if not found).</param>
/// <param name="Importo">Total amount (null if not found).</param>
/// <param name="Descrizione">Merchant name or receipt description.</param>
/// <param name="CategoriaHint">Suggested category keyword (e.g. "trasporto", "pasto", "carburante").</param>
public sealed record ReceiptAnalysisResult(
    DateOnly? Data,
    decimal? Importo,
    string? Descrizione,
    string? CategoriaHint);
