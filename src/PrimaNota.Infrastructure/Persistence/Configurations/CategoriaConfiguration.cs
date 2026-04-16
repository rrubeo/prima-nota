using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PrimaNota.Domain.PianoConti;

namespace PrimaNota.Infrastructure.Persistence.Configurations;

internal sealed class CategoriaConfiguration : IEntityTypeConfiguration<Categoria>
{
    public void Configure(EntityTypeBuilder<Categoria> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("Categorie");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();
        builder.Property(c => c.Codice).IsRequired().HasMaxLength(32);
        builder.Property(c => c.Nome).IsRequired().HasMaxLength(200);
        builder.Property(c => c.Natura).HasConversion<string>().HasMaxLength(16).IsRequired();
        builder.Property(c => c.Attiva).IsRequired();
        builder.Property(c => c.Note).HasMaxLength(500);

        builder.Property(c => c.CreatedAt).IsRequired();
        builder.Property(c => c.CreatedBy).HasMaxLength(450);
        builder.Property(c => c.UpdatedAt);
        builder.Property(c => c.UpdatedBy).HasMaxLength(450);

        builder.HasIndex(c => c.Codice).IsUnique();
        builder.HasIndex(c => new { c.Natura, c.Attiva });
    }
}
