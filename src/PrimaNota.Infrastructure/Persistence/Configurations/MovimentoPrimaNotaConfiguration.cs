using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PrimaNota.Domain.PrimaNota;

namespace PrimaNota.Infrastructure.Persistence.Configurations;

internal sealed class MovimentoPrimaNotaConfiguration : IEntityTypeConfiguration<MovimentoPrimaNota>
{
    public void Configure(EntityTypeBuilder<MovimentoPrimaNota> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("MovimentiPrimaNota");

        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).ValueGeneratedNever();

        builder.Property(m => m.Data).IsRequired();
        builder.Property(m => m.EsercizioAnno).IsRequired();
        builder.Property(m => m.Descrizione).IsRequired().HasMaxLength(500);
        builder.Property(m => m.Numero).HasMaxLength(64);
        builder.Property(m => m.CausaleId).IsRequired();
        builder.Property(m => m.AnagraficaId);
        builder.Property(m => m.Stato).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(m => m.Note).HasMaxLength(2000);

        builder.Property(m => m.RowVersion)
            .IsRowVersion()
            .IsConcurrencyToken();

        builder.Property(m => m.CreatedAt).IsRequired();
        builder.Property(m => m.CreatedBy).HasMaxLength(450);
        builder.Property(m => m.UpdatedAt);
        builder.Property(m => m.UpdatedBy).HasMaxLength(450);

        builder.HasIndex(m => m.EsercizioAnno);
        builder.HasIndex(m => new { m.EsercizioAnno, m.Data });
        builder.HasIndex(m => m.Stato);
        builder.HasIndex(m => m.CausaleId);
        builder.HasIndex(m => m.AnagraficaId);

        builder.HasOne<Domain.PianoConti.Causale>()
            .WithMany()
            .HasForeignKey(m => m.CausaleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Domain.Anagrafiche.Anagrafica>()
            .WithMany()
            .HasForeignKey(m => m.AnagraficaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Domain.Esercizi.EsercizioContabile>()
            .WithMany()
            .HasForeignKey(m => m.EsercizioAnno)
            .HasPrincipalKey(e => e.Anno)
            .OnDelete(DeleteBehavior.Restrict);

        // Owned collection: Righe
        builder.OwnsMany(m => m.Righe, r =>
        {
            r.ToTable("RigheMovimento");
            r.WithOwner().HasForeignKey(x => x.MovimentoId);
            r.HasKey(x => x.Id);
            r.Property(x => x.Id).ValueGeneratedNever();
            r.Property(x => x.MovimentoId).IsRequired();
            r.Property(x => x.Importo).HasPrecision(18, 2).IsRequired();
            r.Property(x => x.ContoFinanziarioId).IsRequired();
            r.Property(x => x.CategoriaId).IsRequired();
            r.Property(x => x.AnagraficaId);
            r.Property(x => x.AliquotaIvaId);
            r.Property(x => x.Note).HasMaxLength(500);

            r.HasIndex(x => x.ContoFinanziarioId);
            r.HasIndex(x => x.CategoriaId);
            r.HasIndex(x => x.AnagraficaId);
            r.HasIndex(x => x.AliquotaIvaId);

            r.HasOne<Domain.ContiFinanziari.ContoFinanziario>()
                .WithMany()
                .HasForeignKey(x => x.ContoFinanziarioId)
                .OnDelete(DeleteBehavior.Restrict);

            r.HasOne<Domain.PianoConti.Categoria>()
                .WithMany()
                .HasForeignKey(x => x.CategoriaId)
                .OnDelete(DeleteBehavior.Restrict);

            r.HasOne<Domain.Anagrafiche.Anagrafica>()
                .WithMany()
                .HasForeignKey(x => x.AnagraficaId)
                .OnDelete(DeleteBehavior.Restrict);

            r.HasOne<Domain.Iva.AliquotaIva>()
                .WithMany()
                .HasForeignKey(x => x.AliquotaIvaId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Owned collection: Allegati
        builder.OwnsMany(m => m.Allegati, a =>
        {
            a.ToTable("AllegatiMovimento");
            a.WithOwner().HasForeignKey(x => x.MovimentoId);
            a.HasKey(x => x.Id);
            a.Property(x => x.Id).ValueGeneratedNever();
            a.Property(x => x.MovimentoId).IsRequired();
            a.Property(x => x.NomeFile).IsRequired().HasMaxLength(260);
            a.Property(x => x.MimeType).IsRequired().HasMaxLength(100);
            a.Property(x => x.Size).IsRequired();
            a.Property(x => x.HashSha256).IsRequired().HasMaxLength(64).IsFixedLength();
            a.Property(x => x.PathRelativo).IsRequired().HasMaxLength(500);
            a.Property(x => x.UploadedAt).IsRequired();
            a.Property(x => x.UploadedBy).HasMaxLength(450);

            a.HasIndex(x => x.HashSha256);
        });
    }
}
