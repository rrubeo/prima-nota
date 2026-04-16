using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PrimaNota.Domain.Iva;

namespace PrimaNota.Infrastructure.Persistence.Configurations;

internal sealed class AliquotaIvaConfiguration : IEntityTypeConfiguration<AliquotaIva>
{
    public void Configure(EntityTypeBuilder<AliquotaIva> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("AliquoteIva");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedNever();
        builder.Property(a => a.Codice).IsRequired().HasMaxLength(16);
        builder.Property(a => a.Descrizione).IsRequired().HasMaxLength(200);
        builder.Property(a => a.Percentuale).HasPrecision(5, 2).IsRequired();
        builder.Property(a => a.PercentualeIndetraibile).HasPrecision(5, 2).IsRequired();
        builder.Property(a => a.Tipo).HasConversion<string>().HasMaxLength(24).IsRequired();
        builder.Property(a => a.CodiceNatura).HasMaxLength(8);
        builder.Property(a => a.Attiva).IsRequired();

        builder.Property(a => a.CreatedAt).IsRequired();
        builder.Property(a => a.CreatedBy).HasMaxLength(450);
        builder.Property(a => a.UpdatedAt);
        builder.Property(a => a.UpdatedBy).HasMaxLength(450);

        builder.HasIndex(a => a.Codice).IsUnique();
        builder.HasIndex(a => new { a.Tipo, a.Attiva });
    }
}
