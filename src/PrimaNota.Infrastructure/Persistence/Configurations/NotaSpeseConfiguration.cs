using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PrimaNota.Domain.NoteSpese;

namespace PrimaNota.Infrastructure.Persistence.Configurations;

internal sealed class NotaSpeseConfiguration : IEntityTypeConfiguration<NotaSpese>
{
    public void Configure(EntityTypeBuilder<NotaSpese> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("NoteSpese");

        builder.HasKey(n => n.Id);
        builder.Property(n => n.Id).ValueGeneratedNever();
        builder.Property(n => n.DipendenteId).IsRequired();
        builder.Property(n => n.Mese).IsRequired();
        builder.Property(n => n.Anno).IsRequired();
        builder.Property(n => n.Descrizione).IsRequired().HasMaxLength(500);
        builder.Property(n => n.Stato).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(n => n.MotivoRifiuto).HasMaxLength(1000);
        builder.Property(n => n.MovimentoRimborsoId);

        builder.Property(n => n.CreatedAt).IsRequired();
        builder.Property(n => n.CreatedBy).HasMaxLength(450);
        builder.Property(n => n.UpdatedAt);
        builder.Property(n => n.UpdatedBy).HasMaxLength(450);

        builder.HasIndex(n => n.DipendenteId);
        builder.HasIndex(n => new { n.Anno, n.Mese });
        builder.HasIndex(n => n.Stato);

        builder.HasOne<Domain.Anagrafiche.Anagrafica>()
            .WithMany()
            .HasForeignKey(n => n.DipendenteId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.OwnsMany(n => n.Righe, r =>
        {
            r.ToTable("RigheSpesa");
            r.WithOwner().HasForeignKey(x => x.NotaSpeseId);
            r.HasKey(x => x.Id);
            r.Property(x => x.Id).ValueGeneratedNever();
            r.Property(x => x.NotaSpeseId).IsRequired();
            r.Property(x => x.Data).IsRequired();
            r.Property(x => x.Descrizione).IsRequired().HasMaxLength(500);
            r.Property(x => x.Importo).HasPrecision(18, 2).IsRequired();
            r.Property(x => x.CategoriaId).IsRequired();
            r.Property(x => x.TipoPagamento).HasConversion<string>().HasMaxLength(20).IsRequired();
            r.Property(x => x.AllegatoPath).HasMaxLength(500);

            r.HasIndex(x => x.CategoriaId);
        });
    }
}
