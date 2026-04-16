using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrimaNota.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEsercizioIvaConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CoefficienteRedditivitaForfettario",
                schema: "app",
                table: "Esercizi",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PeriodicitaIva",
                schema: "app",
                table: "Esercizi",
                type: "nvarchar(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "Trimestrale");

            migrationBuilder.AddColumn<string>(
                name: "RegimeIva",
                schema: "app",
                table: "Esercizi",
                type: "nvarchar(24)",
                maxLength: 24,
                nullable: false,
                defaultValue: "Ordinario");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CoefficienteRedditivitaForfettario",
                schema: "app",
                table: "Esercizi");

            migrationBuilder.DropColumn(
                name: "PeriodicitaIva",
                schema: "app",
                table: "Esercizi");

            migrationBuilder.DropColumn(
                name: "RegimeIva",
                schema: "app",
                table: "Esercizi");
        }
    }
}
