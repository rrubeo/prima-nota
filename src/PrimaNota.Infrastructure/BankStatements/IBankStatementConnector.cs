using PrimaNota.Application.Abstractions;

namespace PrimaNota.Infrastructure.BankStatements;

/// <summary>
/// A connector that knows how to read the bank-statement export of a single institute/format
/// (e.g. Poste Italiane BancoPosta CSV). Adding a new bank means adding one implementation and
/// registering it in DI — nothing else changes.
/// </summary>
public interface IBankStatementConnector
{
    /// <summary>Gets the stable identifier used as manual-override key (e.g. <c>bancoposta-csv</c>).</summary>
    string Id { get; }

    /// <summary>Gets the human-readable name shown in the UI.</summary>
    string DisplayName { get; }

    /// <summary>
    /// Returns <see langword="true"/> when this connector recognizes the file (auto-detection).
    /// </summary>
    /// <param name="fileName">Original file name.</param>
    /// <param name="content">Decoded textual content of the file.</param>
    bool CanParse(string fileName, string content);

    /// <summary>Parses the decoded file content into structured rows.</summary>
    /// <param name="content">Decoded textual content of the file.</param>
    EstratoContoParseResult Parse(string content);
}
