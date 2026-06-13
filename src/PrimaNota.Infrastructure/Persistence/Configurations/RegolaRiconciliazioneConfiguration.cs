using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PrimaNota.Domain.ContiFinanziari;

namespace PrimaNota.Infrastructure.Persistence.Configurations;

internal sealed class RegolaRiconciliazioneConfiguration : IEntityTypeConfiguration<RegolaRiconciliazione>
{
    public void Configure(EntityTypeBuilder<RegolaRiconciliazione> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("RegoleRiconciliazione");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever();
        builder.Property(r => r.ContoFinanziarioId).IsRequired();

        builder.Property(r => r.CausaleOperazione).IsRequired().HasMaxLength(32);
        builder.Property(r => r.Operazione).IsRequired().HasMaxLength(200);
        builder.Property(r => r.DescrizioneChiave).IsRequired().HasMaxLength(200);

        builder.Property(r => r.CausaleId).IsRequired();
        builder.Property(r => r.CategoriaId).IsRequired();
        builder.Property(r => r.AnagraficaId);
        builder.Property(r => r.AliquotaIvaId);
        builder.Property(r => r.ContoDestinazioneId);
        builder.Property(r => r.UtilizziCount).IsRequired();

        builder.Property(r => r.CreatedAt).IsRequired();
        builder.Property(r => r.CreatedBy).HasMaxLength(450);
        builder.Property(r => r.UpdatedAt);
        builder.Property(r => r.UpdatedBy).HasMaxLength(450);

        // One rule per (account + signature): the signature components are non-null
        // (empty string when absent), so SQL Server enforces uniqueness cleanly.
        builder.HasIndex(r => new { r.ContoFinanziarioId, r.CausaleOperazione, r.Operazione, r.DescrizioneChiave })
            .IsUnique();

        builder.HasOne<ContoFinanziario>()
            .WithMany()
            .HasForeignKey(r => r.ContoFinanziarioId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
