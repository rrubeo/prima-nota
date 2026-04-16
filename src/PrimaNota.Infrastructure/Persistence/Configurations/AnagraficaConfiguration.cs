using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PrimaNota.Domain.Anagrafiche;

namespace PrimaNota.Infrastructure.Persistence.Configurations;

internal sealed class AnagraficaConfiguration : IEntityTypeConfiguration<Anagrafica>
{
    public void Configure(EntityTypeBuilder<Anagrafica> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("Anagrafiche");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedNever();

        builder.Property(a => a.RagioneSociale).IsRequired().HasMaxLength(200);
        builder.Property(a => a.Nome).HasMaxLength(100);
        builder.Property(a => a.Cognome).HasMaxLength(100);
        builder.Property(a => a.CodiceFiscale).HasMaxLength(16);
        builder.Property(a => a.PartitaIva).HasMaxLength(16);
        builder.Property(a => a.PersonaFisica).IsRequired();

        builder.Property(a => a.IsCliente).IsRequired();
        builder.Property(a => a.IsFornitore).IsRequired();
        builder.Property(a => a.IsDipendente).IsRequired();

        builder.Property(a => a.Mansione).HasMaxLength(100);
        builder.Property(a => a.DataAssunzione);
        builder.Property(a => a.DataCessazione);

        builder.OwnsOne(a => a.Contatti, c =>
        {
            c.Property(x => x.Email).HasColumnName("Email").HasMaxLength(254);
            c.Property(x => x.Telefono).HasColumnName("Telefono").HasMaxLength(32);
            c.Property(x => x.Pec).HasColumnName("Pec").HasMaxLength(254);
        });

        builder.OwnsOne(a => a.Indirizzo, i =>
        {
            i.Property(x => x.Via).HasColumnName("IndirizzoVia").HasMaxLength(200);
            i.Property(x => x.Cap).HasColumnName("IndirizzoCap").HasMaxLength(10);
            i.Property(x => x.Citta).HasColumnName("IndirizzoCitta").HasMaxLength(100);
            i.Property(x => x.Provincia).HasColumnName("IndirizzoProvincia").HasMaxLength(4);
            i.Property(x => x.CountryCode).HasColumnName("IndirizzoCountryCode").HasMaxLength(2).IsRequired();
        });

        builder.Property(a => a.Attivo).IsRequired();
        builder.Property(a => a.Note).HasMaxLength(2000);

        builder.Property(a => a.CreatedAt).IsRequired();
        builder.Property(a => a.CreatedBy).HasMaxLength(450);
        builder.Property(a => a.UpdatedAt);
        builder.Property(a => a.UpdatedBy).HasMaxLength(450);

        builder.HasIndex(a => a.RagioneSociale);
        builder.HasIndex(a => a.CodiceFiscale);
        builder.HasIndex(a => a.PartitaIva);
        builder.HasIndex(a => new { a.IsCliente, a.Attivo });
        builder.HasIndex(a => new { a.IsFornitore, a.Attivo });
        builder.HasIndex(a => new { a.IsDipendente, a.Attivo });
    }
}
