using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrimaNota.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAnagrafiche : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Anagrafiche",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RagioneSociale = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Cognome = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CodiceFiscale = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: true),
                    PartitaIva = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: true),
                    PersonaFisica = table.Column<bool>(type: "bit", nullable: false),
                    IsCliente = table.Column<bool>(type: "bit", nullable: false),
                    IsFornitore = table.Column<bool>(type: "bit", nullable: false),
                    IsDipendente = table.Column<bool>(type: "bit", nullable: false),
                    Mansione = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DataAssunzione = table.Column<DateOnly>(type: "date", nullable: true),
                    DataCessazione = table.Column<DateOnly>(type: "date", nullable: true),
                    Email = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: true),
                    Telefono = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    Pec = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: true),
                    IndirizzoVia = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IndirizzoCap = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    IndirizzoCitta = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IndirizzoProvincia = table.Column<string>(type: "nvarchar(4)", maxLength: 4, nullable: true),
                    IndirizzoCountryCode = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: false),
                    Attivo = table.Column<bool>(type: "bit", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Anagrafiche", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Anagrafiche_CodiceFiscale",
                schema: "app",
                table: "Anagrafiche",
                column: "CodiceFiscale");

            migrationBuilder.CreateIndex(
                name: "IX_Anagrafiche_IsCliente_Attivo",
                schema: "app",
                table: "Anagrafiche",
                columns: new[] { "IsCliente", "Attivo" });

            migrationBuilder.CreateIndex(
                name: "IX_Anagrafiche_IsDipendente_Attivo",
                schema: "app",
                table: "Anagrafiche",
                columns: new[] { "IsDipendente", "Attivo" });

            migrationBuilder.CreateIndex(
                name: "IX_Anagrafiche_IsFornitore_Attivo",
                schema: "app",
                table: "Anagrafiche",
                columns: new[] { "IsFornitore", "Attivo" });

            migrationBuilder.CreateIndex(
                name: "IX_Anagrafiche_PartitaIva",
                schema: "app",
                table: "Anagrafiche",
                column: "PartitaIva");

            migrationBuilder.CreateIndex(
                name: "IX_Anagrafiche_RagioneSociale",
                schema: "app",
                table: "Anagrafiche",
                column: "RagioneSociale");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Anagrafiche",
                schema: "app");
        }
    }
}
