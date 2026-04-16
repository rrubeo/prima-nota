using PrimaNota.Domain.Abstractions;

namespace PrimaNota.Domain.PrimaNota;

/// <summary>
/// Prima-nota movement aggregate. Anchors a transactional event (invoice collected,
/// payment made, transfer, ...) to a business date and a fiscal year, and owns one or
/// more <see cref="RigaMovimento"/> lines that describe the imputation. Enforces
/// the state transitions (Draft → Confirmed → Reconciled) and invariants on
/// line balance for internal transfers.
/// </summary>
public sealed class MovimentoPrimaNota : AuditableEntity<Guid>
{
    private readonly List<RigaMovimento> righe = new();
    private readonly List<Allegato> allegati = new();

    /// <summary>Initializes a new instance of the <see cref="MovimentoPrimaNota"/> class.</summary>
    /// <param name="data">Business date (movement date shown to users).</param>
    /// <param name="anno">Fiscal year the movement belongs to.</param>
    /// <param name="descrizione">Short description.</param>
    /// <param name="causaleId">Causale id.</param>
    public MovimentoPrimaNota(DateOnly data, int anno, string descrizione, Guid causaleId)
    {
        if (string.IsNullOrWhiteSpace(descrizione))
        {
            throw new ArgumentException("Descrizione obbligatoria.", nameof(descrizione));
        }

        if (causaleId == Guid.Empty)
        {
            throw new ArgumentException("Causale obbligatoria.", nameof(causaleId));
        }

        if (data.Year != anno)
        {
            throw new ArgumentException($"La data {data:yyyy-MM-dd} non appartiene all'esercizio {anno}.", nameof(data));
        }

        Id = Guid.NewGuid();
        Data = data;
        EsercizioAnno = anno;
        Descrizione = descrizione.Trim();
        CausaleId = causaleId;
        Stato = StatoMovimento.Draft;
    }

    /// <summary>Initializes a new instance of the <see cref="MovimentoPrimaNota"/> class for EF Core.</summary>
    private MovimentoPrimaNota()
    {
    }

    /// <summary>Gets the movement date (business date).</summary>
    public DateOnly Data { get; private set; }

    /// <summary>Gets the fiscal year this movement belongs to.</summary>
    public int EsercizioAnno { get; private set; }

    /// <summary>Gets the short description.</summary>
    public string Descrizione { get; private set; } = string.Empty;

    /// <summary>Gets the optional reference number (invoice number, receipt number, ...).</summary>
    public string? Numero { get; private set; }

    /// <summary>Gets the causale id.</summary>
    public Guid CausaleId { get; private set; }

    /// <summary>Gets the optional default counterparty for the movement.</summary>
    public Guid? AnagraficaId { get; private set; }

    /// <summary>Gets the movement state.</summary>
    public StatoMovimento Stato { get; private set; }

    /// <summary>Gets the optional free-form notes.</summary>
    public string? Note { get; private set; }

    /// <summary>Gets the row-version concurrency token.</summary>
    public byte[] RowVersion { get; private set; } = Array.Empty<byte>();

    /// <summary>Gets the collection of lines owned by this movement.</summary>
    public IReadOnlyList<RigaMovimento> Righe => righe;

    /// <summary>Gets the collection of attachments owned by this movement.</summary>
    public IReadOnlyList<Allegato> Allegati => allegati;

    /// <summary>Gets the signed total amount (sum of line amounts). Zero for internal transfers.</summary>
    public decimal Totale => righe.Sum(r => r.Importo);

    /// <summary>
    /// Gets a value indicating whether the movement is an internal transfer (giroconto).
    /// A transfer has &gt;= 2 lines on different accounts and sums to zero.
    /// </summary>
    public bool IsGiroconto =>
        righe.Count >= 2 &&
        righe.Select(r => r.ContoFinanziarioId).Distinct().Count() >= 2 &&
        Totale == 0m;

    /// <summary>Updates header fields. Only allowed while in Draft state.</summary>
    /// <param name="data">New date.</param>
    /// <param name="descrizione">New description.</param>
    /// <param name="causaleId">New causale.</param>
    /// <param name="numero">New reference number.</param>
    /// <param name="anagraficaId">Default counterparty.</param>
    /// <param name="note">Notes.</param>
    public void UpdateHeader(
        DateOnly data,
        string descrizione,
        Guid causaleId,
        string? numero,
        Guid? anagraficaId,
        string? note)
    {
        EnsureEditable();

        if (string.IsNullOrWhiteSpace(descrizione))
        {
            throw new ArgumentException("Descrizione obbligatoria.", nameof(descrizione));
        }

        if (causaleId == Guid.Empty)
        {
            throw new ArgumentException("Causale obbligatoria.", nameof(causaleId));
        }

        if (data.Year != EsercizioAnno)
        {
            throw new ArgumentException(
                $"La data {data:yyyy-MM-dd} non appartiene all'esercizio {EsercizioAnno}.",
                nameof(data));
        }

        Data = data;
        Descrizione = descrizione.Trim();
        CausaleId = causaleId;
        Numero = string.IsNullOrWhiteSpace(numero) ? null : numero.Trim();
        AnagraficaId = anagraficaId;
        Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
    }

    /// <summary>Replaces the full set of lines atomically (typical edit pattern from UI).</summary>
    /// <param name="lines">New line set (at least one required).</param>
    public void ReplaceRighe(IEnumerable<RigaMovimento> lines)
    {
        ArgumentNullException.ThrowIfNull(lines);
        EnsureEditable();

        var fresh = lines.ToList();
        if (fresh.Count == 0)
        {
            throw new InvalidOperationException("Almeno una riga è obbligatoria.");
        }

        righe.Clear();
        foreach (var line in fresh)
        {
            line.MovimentoId = Id;
            righe.Add(line);
        }
    }

    /// <summary>Attaches a document to this movement (allowed also on Confirmed state).</summary>
    /// <param name="allegato">Attachment.</param>
    public void AddAllegato(Allegato allegato)
    {
        ArgumentNullException.ThrowIfNull(allegato);

        if (Stato == StatoMovimento.Reconciled)
        {
            throw new InvalidOperationException("Impossibile aggiungere allegati a un movimento riconciliato.");
        }

        allegato.MovimentoId = Id;
        allegati.Add(allegato);
    }

    /// <summary>Removes an attachment (Draft or Confirmed; forbidden on Reconciled).</summary>
    /// <param name="allegatoId">Attachment id.</param>
    /// <returns>The removed attachment, or null if not found.</returns>
    public Allegato? RemoveAllegato(Guid allegatoId)
    {
        if (Stato == StatoMovimento.Reconciled)
        {
            throw new InvalidOperationException("Impossibile rimuovere allegati da un movimento riconciliato.");
        }

        var match = allegati.FirstOrDefault(a => a.Id == allegatoId);
        if (match is not null)
        {
            allegati.Remove(match);
        }

        return match;
    }

    /// <summary>Transitions from Draft to Confirmed. Validates invariants.</summary>
    public void Confirm()
    {
        if (Stato != StatoMovimento.Draft)
        {
            throw new InvalidOperationException($"Impossibile confermare un movimento in stato {Stato}.");
        }

        if (righe.Count == 0)
        {
            throw new InvalidOperationException("Almeno una riga è obbligatoria per confermare il movimento.");
        }

        // Multi-account transfers must balance to zero.
        if (righe.Select(r => r.ContoFinanziarioId).Distinct().Count() >= 2 && Totale != 0m)
        {
            throw new InvalidOperationException(
                $"Movimento a piu conti non in pareggio (saldo {Totale:N2}). " +
                "Per un giroconto la somma delle righe deve essere zero.");
        }

        Stato = StatoMovimento.Confirmed;
    }

    /// <summary>Reverts a Confirmed movement back to Draft (only before reconciliation).</summary>
    public void Unconfirm()
    {
        if (Stato == StatoMovimento.Reconciled)
        {
            throw new InvalidOperationException("Impossibile ritornare in Draft un movimento riconciliato.");
        }

        if (Stato != StatoMovimento.Confirmed)
        {
            return;
        }

        Stato = StatoMovimento.Draft;
    }

    /// <summary>Marks the movement as reconciled with a bank statement row (called by module 10).</summary>
    public void MarkReconciled()
    {
        if (Stato != StatoMovimento.Confirmed)
        {
            throw new InvalidOperationException($"Solo i movimenti confermati possono essere riconciliati ({Stato}).");
        }

        Stato = StatoMovimento.Reconciled;
    }

    /// <summary>Reverts a reconciled movement back to Confirmed (undo riconciliazione).</summary>
    public void UnmarkReconciled()
    {
        if (Stato != StatoMovimento.Reconciled)
        {
            return;
        }

        Stato = StatoMovimento.Confirmed;
    }

    private void EnsureEditable()
    {
        if (Stato != StatoMovimento.Draft)
        {
            throw new InvalidOperationException(
                $"Il movimento e in stato {Stato}: per modificarlo riportalo in Draft (sconferma).");
        }
    }
}
