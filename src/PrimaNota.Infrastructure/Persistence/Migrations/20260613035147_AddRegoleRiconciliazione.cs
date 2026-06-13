using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrimaNota.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRegoleRiconciliazione : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RegoleRiconciliazione",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContoFinanziarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CausaleOperazione = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Operazione = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DescrizioneChiave = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CausaleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CategoriaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AnagraficaId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AliquotaIvaId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ContoDestinazioneId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UtilizziCount = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegoleRiconciliazione", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RegoleRiconciliazione_ContiFinanziari_ContoFinanziarioId",
                        column: x => x.ContoFinanziarioId,
                        principalSchema: "app",
                        principalTable: "ContiFinanziari",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RegoleRiconciliazione_ContoFinanziarioId_CausaleOperazione_Operazione_DescrizioneChiave",
                schema: "app",
                table: "RegoleRiconciliazione",
                columns: new[] { "ContoFinanziarioId", "CausaleOperazione", "Operazione", "DescrizioneChiave" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RegoleRiconciliazione",
                schema: "app");
        }
    }
}
