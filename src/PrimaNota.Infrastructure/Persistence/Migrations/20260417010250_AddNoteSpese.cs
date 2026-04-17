using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrimaNota.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddNoteSpese : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NoteSpese",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DipendenteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Mese = table.Column<int>(type: "int", nullable: false),
                    Anno = table.Column<int>(type: "int", nullable: false),
                    Descrizione = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Stato = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    MotivoRifiuto = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    MovimentoRimborsoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NoteSpese", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NoteSpese_Anagrafiche_DipendenteId",
                        column: x => x.DipendenteId,
                        principalSchema: "app",
                        principalTable: "Anagrafiche",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RigheSpesa",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NotaSpeseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Data = table.Column<DateOnly>(type: "date", nullable: false),
                    Descrizione = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Importo = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CategoriaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TipoPagamento = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    AllegatoPath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RigheSpesa", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RigheSpesa_NoteSpese_NotaSpeseId",
                        column: x => x.NotaSpeseId,
                        principalSchema: "app",
                        principalTable: "NoteSpese",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NoteSpese_Anno_Mese",
                schema: "app",
                table: "NoteSpese",
                columns: new[] { "Anno", "Mese" });

            migrationBuilder.CreateIndex(
                name: "IX_NoteSpese_DipendenteId",
                schema: "app",
                table: "NoteSpese",
                column: "DipendenteId");

            migrationBuilder.CreateIndex(
                name: "IX_NoteSpese_Stato",
                schema: "app",
                table: "NoteSpese",
                column: "Stato");

            migrationBuilder.CreateIndex(
                name: "IX_RigheSpesa_CategoriaId",
                schema: "app",
                table: "RigheSpesa",
                column: "CategoriaId");

            migrationBuilder.CreateIndex(
                name: "IX_RigheSpesa_NotaSpeseId",
                schema: "app",
                table: "RigheSpesa",
                column: "NotaSpeseId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RigheSpesa",
                schema: "app");

            migrationBuilder.DropTable(
                name: "NoteSpese",
                schema: "app");
        }
    }
}
