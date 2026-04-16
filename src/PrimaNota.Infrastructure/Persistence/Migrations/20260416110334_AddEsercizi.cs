using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrimaNota.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEsercizi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "app");

            migrationBuilder.CreateTable(
                name: "Esercizi",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    Anno = table.Column<int>(type: "int", nullable: false),
                    DataInizio = table.Column<DateOnly>(type: "date", nullable: false),
                    DataFine = table.Column<DateOnly>(type: "date", nullable: false),
                    Stato = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DataChiusura = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Esercizi", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Esercizi_Anno",
                schema: "app",
                table: "Esercizi",
                column: "Anno",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Esercizi",
                schema: "app");
        }
    }
}
