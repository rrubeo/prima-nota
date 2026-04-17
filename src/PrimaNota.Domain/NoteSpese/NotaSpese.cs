using PrimaNota.Domain.Abstractions;

namespace PrimaNota.Domain.NoteSpese;

/// <summary>
/// Expense report aggregate. An employee creates a nota spese, adds expense lines
/// (each with a receipt), submits it for approval. A contabile approves or rejects it.
/// On approval, a reimbursement movement is auto-generated in prima nota.
/// </summary>
public sealed class NotaSpese : AuditableEntity<Guid>
{
    private readonly List<RigaSpesa> righe = new();

    /// <summary>Initializes a new instance of the <see cref="NotaSpese"/> class.</summary>
    /// <param name="dipendenteId">Employee anagrafica id.</param>
    /// <param name="mese">Reference month.</param>
    /// <param name="anno">Reference year.</param>
    /// <param name="descrizione">Short description (e.g. "Trasferta Milano marzo 2026").</param>
    public NotaSpese(Guid dipendenteId, int mese, int anno, string descrizione)
    {
        if (dipendenteId == Guid.Empty)
        {
            throw new ArgumentException("Dipendente obbligatorio.", nameof(dipendenteId));
        }

        if (string.IsNullOrWhiteSpace(descrizione))
        {
            throw new ArgumentException("Descrizione obbligatoria.", nameof(descrizione));
        }

        Id = Guid.NewGuid();
        DipendenteId = dipendenteId;
        Mese = mese;
        Anno = anno;
        Descrizione = descrizione.Trim();
        Stato = StatoNotaSpese.Bozza;
    }

    private NotaSpese()
    {
    }

    /// <summary>Gets the employee who owns this expense report.</summary>
    public Guid DipendenteId { get; private set; }

    /// <summary>Gets the reference month (1-12).</summary>
    public int Mese { get; private set; }

    /// <summary>Gets the reference year.</summary>
    public int Anno { get; private set; }

    /// <summary>Gets the description.</summary>
    public string Descrizione { get; private set; } = string.Empty;

    /// <summary>Gets the workflow state.</summary>
    public StatoNotaSpese Stato { get; private set; }

    /// <summary>Gets the rejection reason (when state = Rifiutata).</summary>
    public string? MotivoRifiuto { get; private set; }

    /// <summary>Gets the generated reimbursement movement id (when state = Rimborsata).</summary>
    public Guid? MovimentoRimborsoId { get; private set; }

    /// <summary>Gets the expense lines.</summary>
    public IReadOnlyList<RigaSpesa> Righe => righe;

    /// <summary>Gets the total amount to reimburse (sum of lines paid with own money).</summary>
    public decimal TotaleRimborso => righe
        .Where(r => r.TipoPagamento == TipoPagamentoSpesa.MezzoProprio)
        .Sum(r => r.Importo);

    /// <summary>Gets the grand total (all lines regardless of payment method).</summary>
    public decimal Totale => righe.Sum(r => r.Importo);

    /// <summary>Updates the header fields. Only allowed in Bozza or Rifiutata state.</summary>
    /// <param name="descrizione">New description.</param>
    /// <param name="mese">Reference month.</param>
    /// <param name="anno">Reference year.</param>
    public void UpdateHeader(string descrizione, int mese, int anno)
    {
        EnsureEditable();

        if (string.IsNullOrWhiteSpace(descrizione))
        {
            throw new ArgumentException("Descrizione obbligatoria.", nameof(descrizione));
        }

        Descrizione = descrizione.Trim();
        Mese = mese;
        Anno = anno;
    }

    /// <summary>Replaces all expense lines atomically.</summary>
    /// <param name="lines">New lines (at least one).</param>
    public void ReplaceRighe(IEnumerable<RigaSpesa> lines)
    {
        ArgumentNullException.ThrowIfNull(lines);
        EnsureEditable();

        var fresh = lines.ToList();
        if (fresh.Count == 0)
        {
            throw new InvalidOperationException("Almeno una riga spesa e obbligatoria.");
        }

        righe.Clear();
        foreach (var r in fresh)
        {
            r.NotaSpeseId = Id;
            righe.Add(r);
        }
    }

    /// <summary>Submits the nota spese for approval.</summary>
    public void Invia()
    {
        if (Stato != StatoNotaSpese.Bozza && Stato != StatoNotaSpese.Rifiutata)
        {
            throw new InvalidOperationException($"Impossibile inviare una nota spese in stato {Stato}.");
        }

        if (righe.Count == 0)
        {
            throw new InvalidOperationException("Almeno una riga spesa e obbligatoria per inviare.");
        }

        MotivoRifiuto = null;
        Stato = StatoNotaSpese.Inviata;
    }

    /// <summary>Approves the nota spese.</summary>
    public void Approva()
    {
        if (Stato != StatoNotaSpese.Inviata)
        {
            throw new InvalidOperationException($"Solo le note spese inviate possono essere approvate ({Stato}).");
        }

        Stato = StatoNotaSpese.Approvata;
    }

    /// <summary>Rejects the nota spese back to the employee.</summary>
    /// <param name="motivo">Rejection reason.</param>
    public void Rifiuta(string motivo)
    {
        if (Stato != StatoNotaSpese.Inviata)
        {
            throw new InvalidOperationException($"Solo le note spese inviate possono essere rifiutate ({Stato}).");
        }

        MotivoRifiuto = string.IsNullOrWhiteSpace(motivo) ? "Rifiutata" : motivo.Trim();
        Stato = StatoNotaSpese.Rifiutata;
    }

    /// <summary>Marks the nota spese as reimbursed, linking the generated movement.</summary>
    /// <param name="movimentoId">Reimbursement movement id.</param>
    public void SegnaRimborsata(Guid movimentoId)
    {
        if (Stato != StatoNotaSpese.Approvata)
        {
            throw new InvalidOperationException($"Solo le note spese approvate possono essere rimborsate ({Stato}).");
        }

        MovimentoRimborsoId = movimentoId;
        Stato = StatoNotaSpese.Rimborsata;
    }

    private void EnsureEditable()
    {
        if (Stato != StatoNotaSpese.Bozza && Stato != StatoNotaSpese.Rifiutata)
        {
            throw new InvalidOperationException(
                $"La nota spese e in stato {Stato}: per modificarla deve essere in Bozza o Rifiutata.");
        }
    }
}
