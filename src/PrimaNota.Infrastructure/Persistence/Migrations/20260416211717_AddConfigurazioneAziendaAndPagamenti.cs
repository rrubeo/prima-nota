using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrimaNota.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddConfigurazioneAziendaAndPagamenti : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "DataCompetenza",
                schema: "app",
                table: "MovimentiPrimaNota",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            // Backfill DataCompetenza on pre-existing rows: VAT competence defaults to the
            // movement registration date until the user overrides it for imported invoices.
            migrationBuilder.Sql(
                "UPDATE [app].[MovimentiPrimaNota] SET [DataCompetenza] = [Data] WHERE [DataCompetenza] = '0001-01-01';");

            migrationBuilder.CreateTable(
                name: "ConfigurazioneAzienda",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    Denominazione = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PartitaIva = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: true),
                    CodiceFiscale = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: true),
                    IndirizzoVia = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IndirizzoCap = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    IndirizzoCitta = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IndirizzoProvincia = table.Column<string>(type: "nvarchar(4)", maxLength: 4, nullable: true),
                    IndirizzoCountryCode = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: true),
                    Telefono = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    Pec = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: true),
                    EsigibilitaIvaPredefinita = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfigurazioneAzienda", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PagamentiMovimento",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MovimentoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Data = table.Column<DateOnly>(type: "date", nullable: false),
                    Importo = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ContoFinanziarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PagamentiMovimento", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PagamentiMovimento_ContiFinanziari_ContoFinanziarioId",
                        column: x => x.ContoFinanziarioId,
                        principalSchema: "app",
                        principalTable: "ContiFinanziari",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PagamentiMovimento_MovimentiPrimaNota_MovimentoId",
                        column: x => x.MovimentoId,
                        principalSchema: "app",
                        principalTable: "MovimentiPrimaNota",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MovimentiPrimaNota_EsercizioAnno_DataCompetenza",
                schema: "app",
                table: "MovimentiPrimaNota",
                columns: new[] { "EsercizioAnno", "DataCompetenza" });

            migrationBuilder.CreateIndex(
                name: "IX_PagamentiMovimento_ContoFinanziarioId",
                schema: "app",
                table: "PagamentiMovimento",
                column: "ContoFinanziarioId");

            migrationBuilder.CreateIndex(
                name: "IX_PagamentiMovimento_Data",
                schema: "app",
                table: "PagamentiMovimento",
                column: "Data");

            migrationBuilder.CreateIndex(
                name: "IX_PagamentiMovimento_MovimentoId",
                schema: "app",
                table: "PagamentiMovimento",
                column: "MovimentoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConfigurazioneAzienda",
                schema: "app");

            migrationBuilder.DropTable(
                name: "PagamentiMovimento",
                schema: "app");

            migrationBuilder.DropIndex(
                name: "IX_MovimentiPrimaNota_EsercizioAnno_DataCompetenza",
                schema: "app",
                table: "MovimentiPrimaNota");

            migrationBuilder.DropColumn(
                name: "DataCompetenza",
                schema: "app",
                table: "MovimentiPrimaNota");
        }
    }
}
