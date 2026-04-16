using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PrimaNota.Domain.Esercizi;

namespace PrimaNota.Infrastructure.Persistence.Configurations;

internal sealed class EsercizioContabileConfiguration : IEntityTypeConfiguration<EsercizioContabile>
{
    public void Configure(EntityTypeBuilder<EsercizioContabile> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("Esercizi");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.Anno).IsRequired();
        builder.Property(e => e.DataInizio).IsRequired();
        builder.Property(e => e.DataFine).IsRequired();
        builder.Property(e => e.Stato)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();
        builder.Property(e => e.DataChiusura);

        builder.Property(e => e.CreatedAt).IsRequired();
        builder.Property(e => e.CreatedBy).HasMaxLength(450);
        builder.Property(e => e.UpdatedAt);
        builder.Property(e => e.UpdatedBy).HasMaxLength(450);

        builder.HasIndex(e => e.Anno).IsUnique();
    }
}
