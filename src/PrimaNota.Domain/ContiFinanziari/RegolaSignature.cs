using System.Globalization;
using System.Text;

namespace PrimaNota.Domain.ContiFinanziari;

/// <summary>
/// Deterministic signature that identifies "the same kind" of bank-statement row, used to
/// memorize and re-apply reconciliation choices. The signature combines the bank cause code
/// and operation name with a normalized fragment of the free-text description: volatile parts
/// (amounts, dates, transaction/IBAN/distinta codes, month names) are stripped so that two
/// rows describing the same counterparty/operation collapse to the same key.
/// </summary>
public static class RegolaSignature
{
    /// <summary>Maximum number of stable description tokens kept in the key.</summary>
    private const int MaxTokens = 3;

    /// <summary>Minimum length for a description token to be considered meaningful.</summary>
    private const int MinTokenLength = 3;

    // Connective / banking noise words and month names: they never identify a counterparty,
    // so dropping them lets recurring fees (whose only variable part is the month) collapse
    // to an empty description key and match on cause + operation alone.
    private static readonly HashSet<string> StopWords = new(StringComparer.Ordinal)
    {
        "per", "del", "dal", "dei", "con", "tra", "the", "via",
        "trn", "cid", "man", "iban", "bic", "carta", "distinta", "conto",
        "eseguita", "eseguito", "addebito", "accredito", "relativo", "periodo",
        "gennaio", "febbraio", "marzo", "aprile", "maggio", "giugno",
        "luglio", "agosto", "settembre", "ottobre", "novembre", "dicembre",
    };

    /// <summary>Computes the signature for a bank-statement row.</summary>
    /// <param name="causaleOperazione">Bank cause code (e.g. <c>48</c>).</param>
    /// <param name="operazione">Bank operation name (e.g. <c>BONIFICO SEPA</c>).</param>
    /// <param name="descrizione">Free-text description.</param>
    /// <returns>The normalized signature key (all components non-null; may be empty).</returns>
    public static RegolaSignatureKey Compute(string? causaleOperazione, string? operazione, string? descrizione)
        => new(
            NormalizeCode(causaleOperazione),
            NormalizeCode(operazione),
            ComputeDescrizioneChiave(descrizione));

    /// <summary>Normalizes a code/keyword field (trim + upper invariant; empty if missing).</summary>
    /// <param name="value">Raw value.</param>
    /// <returns>Normalized value, never null.</returns>
    public static string NormalizeCode(string? value)
        => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().ToUpperInvariant();

    /// <summary>Extracts the stable, normalized description fragment used as part of the key.</summary>
    /// <param name="descrizione">Free-text description.</param>
    /// <returns>Up to <see cref="MaxTokens"/> stable lowercase tokens joined by a space; may be empty.</returns>
    public static string ComputeDescrizioneChiave(string? descrizione)
    {
        if (string.IsNullOrWhiteSpace(descrizione))
        {
            return string.Empty;
        }

        var tokens = new List<string>();
        foreach (var token in Tokenize(descrizione))
        {
            if (token.Length < MinTokenLength || StopWords.Contains(token))
            {
                continue;
            }

            tokens.Add(token);
            if (tokens.Count == MaxTokens)
            {
                break;
            }
        }

        return string.Join(' ', tokens);
    }

    private static IEnumerable<string> Tokenize(string text)
    {
        var sb = new StringBuilder();
        var hasDigit = false;

        foreach (var ch in text)
        {
            if (char.IsLetter(ch))
            {
                sb.Append(char.ToLower(ch, CultureInfo.InvariantCulture));
            }
            else if (char.IsDigit(ch))
            {
                sb.Append(ch);
                hasDigit = true;
            }
            else
            {
                if (sb.Length > 0 && !hasDigit)
                {
                    yield return sb.ToString();
                }

                sb.Clear();
                hasDigit = false;
            }
        }

        if (sb.Length > 0 && !hasDigit)
        {
            yield return sb.ToString();
        }
    }
}
