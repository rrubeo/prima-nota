namespace PrimaNota.Domain.NoteSpese;

/// <summary>How the expense was paid by the employee.</summary>
public enum TipoPagamentoSpesa
{
    /// <summary>Paid with the employee's own money — requires reimbursement.</summary>
    MezzoProprio = 1,

    /// <summary>Paid with a company card/account — no reimbursement needed, but the expense is tracked.</summary>
    MezzoAziendale = 2,
}
