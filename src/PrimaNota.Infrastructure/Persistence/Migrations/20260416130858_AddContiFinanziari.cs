using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrimaNota.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddContiFinanziari : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ContiFinanziari",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Codice = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Istituto = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Iban = table.Column<string>(type: "nvarchar(34)", maxLength: 34, nullable: true),
                    Bic = table.Column<string>(type: "nvarchar(11)", maxLength: 11, nullable: true),
                    Intestatario = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Ultime4Cifre = table.Column<string>(type: "nchar(4)", fixedLength: true, maxLength: 4, nullable: true),
                    SaldoIniziale = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DataSaldoIniziale = table.Column<DateOnly>(type: "date", nullable: false),
                    Valuta = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    Attivo = table.Column<bool>(type: "bit", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContiFinanziari", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ContiFinanziari_Codice",
                schema: "app",
                table: "ContiFinanziari",
                column: "Codice",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ContiFinanziari_Iban",
                schema: "app",
                table: "ContiFinanziari",
                column: "Iban");

            migrationBuilder.CreateIndex(
                name: "IX_ContiFinanziari_Tipo_Attivo",
                schema: "app",
                table: "ContiFinanziari",
                columns: new[] { "Tipo", "Attivo" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ContiFinanziari",
                schema: "app");
        }
    }
}
