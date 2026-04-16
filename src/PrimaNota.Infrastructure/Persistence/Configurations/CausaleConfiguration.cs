using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PrimaNota.Domain.PianoConti;

namespace PrimaNota.Infrastructure.Persistence.Configurations;

internal sealed class CausaleConfiguration : IEntityTypeConfiguration<Causale>
{
    public void Configure(EntityTypeBuilder<Causale> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("Causali");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();
        builder.Property(c => c.Codice).IsRequired().HasMaxLength(32);
        builder.Property(c => c.Nome).IsRequired().HasMaxLength(200);
        builder.Property(c => c.Tipo).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(c => c.CategoriaDefaultId);
        builder.Property(c => c.Attiva).IsRequired();
        builder.Property(c => c.Note).HasMaxLength(500);

        builder.Property(c => c.CreatedAt).IsRequired();
        builder.Property(c => c.CreatedBy).HasMaxLength(450);
        builder.Property(c => c.UpdatedAt);
        builder.Property(c => c.UpdatedBy).HasMaxLength(450);

        builder.HasIndex(c => c.Codice).IsUnique();
        builder.HasIndex(c => new { c.Tipo, c.Attiva });

        builder.HasOne<Categoria>()
            .WithMany()
            .HasForeignKey(c => c.CategoriaDefaultId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
