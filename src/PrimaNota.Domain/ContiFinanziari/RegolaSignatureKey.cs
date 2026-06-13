namespace PrimaNota.Domain.ContiFinanziari;

/// <summary>Normalized key that identifies equivalent bank-statement rows.</summary>
/// <param name="CausaleOperazione">Normalized bank cause code (never null; may be empty).</param>
/// <param name="Operazione">Normalized operation name (never null; may be empty).</param>
/// <param name="DescrizioneChiave">Stable description fragment (never null; may be empty).</param>
public readonly record struct RegolaSignatureKey(string CausaleOperazione, string Operazione, string DescrizioneChiave);
