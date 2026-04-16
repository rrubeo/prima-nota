namespace PrimaNota.Domain.Iva;

/// <summary>VAT treatment classification (Italian tax semantics).</summary>
public enum TipoIva
{
    /// <summary>Ordinary VAT (22%, 10%, 5%, 4%).</summary>
    Ordinaria = 1,

    /// <summary>Exempt operation (art. 10 DPR 633/72).</summary>
    Esente = 2,

    /// <summary>Non-taxable operation (art. 8, 8-bis, 9 DPR 633/72).</summary>
    NonImponibile = 3,

    /// <summary>Outside VAT scope (fuori campo).</summary>
    FuoriCampo = 4,

    /// <summary>Reverse charge (inversione contabile).</summary>
    ReverseCharge = 5,
}
