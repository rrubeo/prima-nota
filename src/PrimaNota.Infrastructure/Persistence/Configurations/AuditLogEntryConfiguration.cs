using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PrimaNota.Domain.Audit;

namespace PrimaNota.Infrastructure.Persistence.Configurations;

internal sealed class AuditLogEntryConfiguration : IEntityTypeConfiguration<AuditLogEntry>
{
    public void Configure(EntityTypeBuilder<AuditLogEntry> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("AuditLog");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.OccurredAt).IsRequired();
        builder.Property(e => e.Kind)
            .HasConversion<string>()
            .HasMaxLength(64)
            .IsRequired();
        builder.Property(e => e.UserId).HasMaxLength(450);
        builder.Property(e => e.UserName).HasMaxLength(256);
        builder.Property(e => e.TargetType).HasMaxLength(256).IsRequired();
        builder.Property(e => e.TargetId).HasMaxLength(64).IsRequired();
        builder.Property(e => e.Summary).HasMaxLength(512).IsRequired();
        builder.Property(e => e.PayloadJson).HasColumnType("nvarchar(max)");
        builder.Property(e => e.CorrelationId).HasMaxLength(128);
        builder.Property(e => e.IpAddress).HasMaxLength(64);

        builder.HasIndex(e => e.OccurredAt);
        builder.HasIndex(e => new { e.UserId, e.OccurredAt });
        builder.HasIndex(e => e.Kind);
    }
}
