using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrimaNota.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPianoContiCausaliIva : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AliquoteIva",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Codice = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    Descrizione = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Percentuale = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    PercentualeIndetraibile = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    CodiceNatura = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: true),
                    Attiva = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AliquoteIva", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Categorie",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Codice = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Natura = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    Attiva = table.Column<bool>(type: "bit", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categorie", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Causali",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Codice = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    CategoriaDefaultId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Attiva = table.Column<bool>(type: "bit", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Causali", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Causali_Categorie_CategoriaDefaultId",
                        column: x => x.CategoriaDefaultId,
                        principalSchema: "app",
                        principalTable: "Categorie",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AliquoteIva_Codice",
                schema: "app",
                table: "AliquoteIva",
                column: "Codice",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AliquoteIva_Tipo_Attiva",
                schema: "app",
                table: "AliquoteIva",
                columns: new[] { "Tipo", "Attiva" });

            migrationBuilder.CreateIndex(
                name: "IX_Categorie_Codice",
                schema: "app",
                table: "Categorie",
                column: "Codice",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Categorie_Natura_Attiva",
                schema: "app",
                table: "Categorie",
                columns: new[] { "Natura", "Attiva" });

            migrationBuilder.CreateIndex(
                name: "IX_Causali_CategoriaDefaultId",
                schema: "app",
                table: "Causali",
                column: "CategoriaDefaultId");

            migrationBuilder.CreateIndex(
                name: "IX_Causali_Codice",
                schema: "app",
                table: "Causali",
                column: "Codice",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Causali_Tipo_Attiva",
                schema: "app",
                table: "Causali",
                columns: new[] { "Tipo", "Attiva" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AliquoteIva",
                schema: "app");

            migrationBuilder.DropTable(
                name: "Causali",
                schema: "app");

            migrationBuilder.DropTable(
                name: "Categorie",
                schema: "app");
        }
    }
}
