using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrimaNota.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEstrattiConto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EstrattiConto",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContoFinanziarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NomeFile = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    PeriodoDa = table.Column<DateOnly>(type: "date", nullable: false),
                    PeriodoA = table.Column<DateOnly>(type: "date", nullable: false),
                    SaldoContabile = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EstrattiConto", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EstrattiConto_ContiFinanziari_ContoFinanziarioId",
                        column: x => x.ContoFinanziarioId,
                        principalSchema: "app",
                        principalTable: "ContiFinanziari",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RigheEstrattoConto",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ImportId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DataContabile = table.Column<DateOnly>(type: "date", nullable: false),
                    DataValuta = table.Column<DateOnly>(type: "date", nullable: false),
                    CausaleOperazione = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    Operazione = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Descrizione = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Importo = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Stato = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    MovimentoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PagamentoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RigheEstrattoConto", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RigheEstrattoConto_EstrattiConto_ImportId",
                        column: x => x.ImportId,
                        principalSchema: "app",
                        principalTable: "EstrattiConto",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EstrattiConto_ContoFinanziarioId",
                schema: "app",
                table: "EstrattiConto",
                column: "ContoFinanziarioId");

            migrationBuilder.CreateIndex(
                name: "IX_RigheEstrattoConto_DataContabile",
                schema: "app",
                table: "RigheEstrattoConto",
                column: "DataContabile");

            migrationBuilder.CreateIndex(
                name: "IX_RigheEstrattoConto_ImportId",
                schema: "app",
                table: "RigheEstrattoConto",
                column: "ImportId");

            migrationBuilder.CreateIndex(
                name: "IX_RigheEstrattoConto_MovimentoId",
                schema: "app",
                table: "RigheEstrattoConto",
                column: "MovimentoId");

            migrationBuilder.CreateIndex(
                name: "IX_RigheEstrattoConto_Stato",
                schema: "app",
                table: "RigheEstrattoConto",
                column: "Stato");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RigheEstrattoConto",
                schema: "app");

            migrationBuilder.DropTable(
                name: "EstrattiConto",
                schema: "app");
        }
    }
}
