using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PrimaNota.Domain.ContiFinanziari;

namespace PrimaNota.Infrastructure.Persistence.Configurations;

internal sealed class ContoFinanziarioConfiguration : IEntityTypeConfiguration<ContoFinanziario>
{
    public void Configure(EntityTypeBuilder<ContoFinanziario> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("ContiFinanziari");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();

        builder.Property(c => c.Codice).IsRequired().HasMaxLength(32);
        builder.Property(c => c.Nome).IsRequired().HasMaxLength(200);
        builder.Property(c => c.Tipo).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(c => c.Istituto).HasMaxLength(200);
        builder.Property(c => c.Iban).HasMaxLength(34);
        builder.Property(c => c.Bic).HasMaxLength(11);
        builder.Property(c => c.Intestatario).HasMaxLength(200);
        builder.Property(c => c.Ultime4Cifre).HasMaxLength(4).IsFixedLength();
        builder.Property(c => c.SaldoIniziale).HasPrecision(18, 2).IsRequired();
        builder.Property(c => c.DataSaldoIniziale).IsRequired();
        builder.Property(c => c.Valuta).HasMaxLength(3).IsRequired();
        builder.Property(c => c.Attivo).IsRequired();
        builder.Property(c => c.Note).HasMaxLength(500);

        builder.Property(c => c.CreatedAt).IsRequired();
        builder.Property(c => c.CreatedBy).HasMaxLength(450);
        builder.Property(c => c.UpdatedAt);
        builder.Property(c => c.UpdatedBy).HasMaxLength(450);

        builder.HasIndex(c => c.Codice).IsUnique();
        builder.HasIndex(c => c.Iban);
        builder.HasIndex(c => new { c.Tipo, c.Attivo });
    }
}
