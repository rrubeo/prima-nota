using PrimaNota.Domain.ContiFinanziari;

namespace PrimaNota.Application.Abstractions;

/// <summary>Parses a bank-statement file into structured rows.</summary>
public interface IEstratoContoParser
{
    /// <summary>Parses the given stream.</summary>
    /// <param name="stream">File content.</param>
    /// <param name="fileName">Original file name (used to infer format).</param>
    /// <returns>Parsed result.</returns>
    EstratoContoParseResult Parse(Stream stream, string fileName);
}

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
