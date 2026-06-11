using System.Text;
using PrimaNota.Application.Abstractions;

namespace PrimaNota.Infrastructure.BankStatements;

/// <summary>
/// <see cref="IEstratoContoParser"/> implementation that routes a bank-statement file to the
/// right <see cref="IBankStatementConnector"/>, either by explicit selection (manual override)
/// or by content-based auto-detection.
/// </summary>
public sealed class EstratoContoParserDispatcher : IEstratoContoParser
{
    private readonly IReadOnlyList<IBankStatementConnector> connectors;
    private readonly IReadOnlyList<BankConnectorInfo> connectorInfos;

    /// <summary>Initializes a new instance of the <see cref="EstratoContoParserDispatcher"/> class.</summary>
    /// <param name="connectors">All registered bank connectors.</param>
    public EstratoContoParserDispatcher(IEnumerable<IBankStatementConnector> connectors)
    {
        ArgumentNullException.ThrowIfNull(connectors);
        this.connectors = connectors.ToList();
        this.connectorInfos = this.connectors.Select(c => new BankConnectorInfo(c.Id, c.DisplayName)).ToList();
    }

    /// <inheritdoc />
    public IReadOnlyList<BankConnectorInfo> AvailableConnectors => connectorInfos;

    /// <inheritdoc />
    public EstratoContoParseResult Parse(Stream stream, string fileName, string? connectorId = null)
    {
        ArgumentNullException.ThrowIfNull(stream);

        var content = ReadAllText(stream);

        IBankStatementConnector connector;
        if (!string.IsNullOrWhiteSpace(connectorId))
        {
            connector = connectors.FirstOrDefault(c => c.Id == connectorId)
                ?? throw new NotSupportedException($"Connettore '{connectorId}' non riconosciuto.");
        }
        else
        {
            connector = connectors.FirstOrDefault(c => c.CanParse(fileName, content))
                ?? throw new NotSupportedException(
                    "Impossibile riconoscere automaticamente l'istituto dal file. Selezionare manualmente l'istituto e riprovare.");
        }

        return connector.Parse(content);
    }

    private static string ReadAllText(Stream stream)
    {
        if (stream.CanSeek)
        {
            stream.Position = 0;
        }

        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
        return reader.ReadToEnd();
    }
}
