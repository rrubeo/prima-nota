namespace PrimaNota.Domain.NoteSpese;

/// <summary>Workflow states for an expense report.</summary>
public enum StatoNotaSpese
{
    /// <summary>Draft: the employee is still editing.</summary>
    Bozza = 1,

    /// <summary>Submitted: waiting for manager / contabile approval.</summary>
    Inviata = 2,

    /// <summary>Approved: the expense report has been validated.</summary>
    Approvata = 3,

    /// <summary>Rejected: sent back to the employee with a reason.</summary>
    Rifiutata = 4,

    /// <summary>Reimbursed: a prima-nota movement has been generated for the reimbursement.</summary>
    Rimborsata = 5,
}
