using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PrimaNota.Domain.Azienda;

namespace PrimaNota.Infrastructure.Persistence.Configurations;

internal sealed class ConfigurazioneAziendaConfiguration : IEntityTypeConfiguration<ConfigurazioneAzienda>
{
    public void Configure(EntityTypeBuilder<ConfigurazioneAzienda> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("ConfigurazioneAzienda");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();
        builder.Property(c => c.Denominazione).IsRequired().HasMaxLength(200);
        builder.Property(c => c.PartitaIva).HasMaxLength(16);
        builder.Property(c => c.CodiceFiscale).HasMaxLength(16);
        builder.Property(c => c.Email).HasMaxLength(254);
        builder.Property(c => c.Telefono).HasMaxLength(32);
        builder.Property(c => c.Pec).HasMaxLength(254);

        builder.Property(c => c.EsigibilitaIvaPredefinita)
            .HasConversion<string>()
            .HasMaxLength(16)
            .IsRequired();

        builder.OwnsOne(c => c.Indirizzo, i =>
        {
            i.Property(x => x.Via).HasColumnName("IndirizzoVia").HasMaxLength(200);
            i.Property(x => x.Cap).HasColumnName("IndirizzoCap").HasMaxLength(10);
            i.Property(x => x.Citta).HasColumnName("IndirizzoCitta").HasMaxLength(100);
            i.Property(x => x.Provincia).HasColumnName("IndirizzoProvincia").HasMaxLength(4);
            i.Property(x => x.CountryCode).HasColumnName("IndirizzoCountryCode").HasMaxLength(2).IsRequired();
        });

        builder.Property(c => c.CreatedAt).IsRequired();
        builder.Property(c => c.CreatedBy).HasMaxLength(450);
        builder.Property(c => c.UpdatedAt);
        builder.Property(c => c.UpdatedBy).HasMaxLength(450);
    }
}
