using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PrimaNota.Domain.ContiFinanziari;

namespace PrimaNota.Infrastructure.Persistence.Configurations;

internal sealed class EstratoContoImportConfiguration : IEntityTypeConfiguration<EstratoContoImport>
{
    public void Configure(EntityTypeBuilder<EstratoContoImport> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("EstrattiConto");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.ContoFinanziarioId).IsRequired();
        builder.Property(e => e.NomeFile).IsRequired().HasMaxLength(260);
        builder.Property(e => e.PeriodoDa).IsRequired();
        builder.Property(e => e.PeriodoA).IsRequired();
        builder.Property(e => e.SaldoContabile).HasPrecision(18, 2);

        builder.Property(e => e.CreatedAt).IsRequired();
        builder.Property(e => e.CreatedBy).HasMaxLength(450);
        builder.Property(e => e.UpdatedAt);
        builder.Property(e => e.UpdatedBy).HasMaxLength(450);

        builder.HasIndex(e => e.ContoFinanziarioId);

        builder.HasOne<ContoFinanziario>()
            .WithMany()
            .HasForeignKey(e => e.ContoFinanziarioId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.OwnsMany(e => e.Righe, r =>
        {
            r.ToTable("RigheEstrattoConto");
            r.WithOwner().HasForeignKey(x => x.ImportId);
            r.HasKey(x => x.Id);
            r.Property(x => x.Id).ValueGeneratedNever();
            r.Property(x => x.ImportId).IsRequired();
            r.Property(x => x.DataContabile).IsRequired();
            r.Property(x => x.DataValuta).IsRequired();
            r.Property(x => x.CausaleOperazione).HasMaxLength(32);
            r.Property(x => x.Operazione).HasMaxLength(200);
            r.Property(x => x.Descrizione).HasMaxLength(1000);
            r.Property(x => x.Importo).HasPrecision(18, 2).IsRequired();
            r.Property(x => x.Stato).HasConversion<string>().HasMaxLength(20).IsRequired();
            r.Property(x => x.MovimentoId);
            r.Property(x => x.PagamentoId);

            r.HasIndex(x => x.DataContabile);
            r.HasIndex(x => x.Stato);
            r.HasIndex(x => x.MovimentoId);
        });
    }
}
