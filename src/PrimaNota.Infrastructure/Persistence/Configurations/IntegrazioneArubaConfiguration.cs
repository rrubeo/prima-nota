using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PrimaNota.Domain.Integrazioni;

namespace PrimaNota.Infrastructure.Persistence.Configurations;

internal sealed class IntegrazioneArubaConfiguration : IEntityTypeConfiguration<IntegrazioneAruba>
{
    public void Configure(EntityTypeBuilder<IntegrazioneAruba> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("IntegrazioneAruba");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();
        builder.Property(c => c.Abilitata).IsRequired();
        builder.Property(c => c.Username).HasMaxLength(256);

        // Encrypted password (Data Protection ciphertext) — generously sized.
        builder.Property(c => c.PasswordProtetta).HasMaxLength(2048);
        builder.Property(c => c.UsaDemo).IsRequired();

        builder.Property(c => c.CreatedAt).IsRequired();
        builder.Property(c => c.CreatedBy).HasMaxLength(450);
        builder.Property(c => c.UpdatedAt);
        builder.Property(c => c.UpdatedBy).HasMaxLength(450);
    }
}
