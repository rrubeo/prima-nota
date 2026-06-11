using PrimaNota.Domain.ContiFinanziari;

namespace PrimaNota.Application.Abstractions;

/// <summary>Parses a bank-statement file into structured rows.</summary>
public interface IEstratoContoParser
{
    /// <summary>Gets the bank connectors available for import (one per institute/format).</summary>
    IReadOnlyList<BankConnectorInfo> AvailableConnectors { get; }

    /// <summary>Parses the given stream.</summary>
    /// <param name="stream">File content.</param>
    /// <param name="fileName">Original file name (used to infer format).</param>
    /// <param name="connectorId">
    /// Explicit connector to use (manual override). When <see langword="null"/> the connector is
    /// auto-detected from the file content.
    /// </param>
    /// <returns>Parsed result.</returns>
    EstratoContoParseResult Parse(Stream stream, string fileName, string? connectorId = null);
}

/// <summary>Describes a bank connector available to the UI.</summary>
/// <param name="Id">Stable connector identifier (e.g. <c>bancoposta-csv</c>).</param>
/// <param name="DisplayName">Human-readable name (e.g. <c>Poste Italiane — BancoPosta (CSV)</c>).</param>
public sealed record BankConnectorInfo(string Id, string DisplayName);

/// <summary>Result of a bank-statement parse operation.</summary>
/// <param name="PeriodoDa">Statement period start.</param>
/// <param name="PeriodoA">Statement period end.</param>
/// <param name="SaldoContabile">Reported balance (null if unavailable).</param>
/// <param name="Righe">Parsed rows.</param>
public sealed record EstratoContoParseResult(
    DateOnly PeriodoDa,
    DateOnly PeriodoA,
    decimal? SaldoContabile,
    IReadOnlyList<RigaEstrattoConto> Righe);
