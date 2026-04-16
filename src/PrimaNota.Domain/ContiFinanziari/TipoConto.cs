namespace PrimaNota.Domain.ContiFinanziari;

/// <summary>Kind of financial account tracked by Prima Nota.</summary>
public enum TipoConto
{
    /// <summary>Physical cash register or petty-cash drawer.</summary>
    Cassa = 1,

    /// <summary>Bank account (current, savings, postal, ...).</summary>
    Banca = 2,

    /// <summary>Credit card (corporate or personal-with-corporate-usage).</summary>
    CartaDiCredito = 3,

    /// <summary>Debit / prepaid / rechargeable card.</summary>
    CartaDebitoPrepagata = 4,
}
