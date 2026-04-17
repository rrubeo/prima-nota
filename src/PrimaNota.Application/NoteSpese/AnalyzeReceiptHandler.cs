using MediatR;
using PrimaNota.Application.Abstractions;

namespace PrimaNota.Application.NoteSpese;

/// <summary>Analyzes a receipt image and returns structured expense data.</summary>
/// <param name="ImageBytes">Raw image bytes.</param>
/// <param name="MimeType">MIME type.</param>
public sealed record AnalyzeReceipt(byte[] ImageBytes, string MimeType) : IRequest<ReceiptAnalysisResult>;

/// <summary>Handler for <see cref="AnalyzeReceipt"/>.</summary>
public sealed class AnalyzeReceiptHandler : IRequestHandler<AnalyzeReceipt, ReceiptAnalysisResult>
{
    private readonly IReceiptAnalyzer analyzer;

    /// <summary>Initializes a new instance of the <see cref="AnalyzeReceiptHandler"/> class.</summary>
    /// <param name="analyzer">Receipt analyzer.</param>
    public AnalyzeReceiptHandler(IReceiptAnalyzer analyzer) => this.analyzer = analyzer;

    /// <inheritdoc />
    public async Task<ReceiptAnalysisResult> Handle(AnalyzeReceipt request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return await analyzer.AnalyzeAsync(request.ImageBytes, request.MimeType, cancellationToken);
    }
}
