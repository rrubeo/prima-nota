namespace PrimaNota.Domain.Iva;

/// <summary>
/// When the VAT on an invoice becomes payable/deductible.
/// Company-wide parameter configured on <c>ConfigurazioneAzienda</c>.
/// </summary>
public enum EsigibilitaIva
{
    /// <summary>VAT is due/deductible in the period of the invoice (data competenza) — default.</summary>
    Immediata = 1,

    /// <summary>
    /// Cash accounting (IVA per cassa, art. 32-bis DL 83/2012): VAT is due/deductible only
    /// in the period when the invoice is actually collected or paid.
    /// </summary>
    Cassa = 2,
}
