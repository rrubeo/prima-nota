using PrimaNota.Application.Abstractions;

namespace PrimaNota.Infrastructure.BankStatements;

/// <summary>
/// <see cref="IEstratoContoParser"/> implementation that delegates to <see cref="BancoPostaParser"/>
/// for PDF files. Future bank formats can be added here with format detection.
/// </summary>
public sealed class PdfEstratoContoParser : IEstratoContoParser
{
    /// <inheritdoc />
    public EstratoContoParseResult Parse(Stream stream, string fileName)
    {
        ArgumentNullException.ThrowIfNull(stream);

        return BancoPostaParser.Parse(stream);
    }
}
