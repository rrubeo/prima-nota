using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrimaNota.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMovimentiPrimaNota : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddUniqueConstraint(
                name: "AK_Esercizi_Anno",
                schema: "app",
                table: "Esercizi",
                column: "Anno");

            migrationBuilder.CreateTable(
                name: "MovimentiPrimaNota",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Data = table.Column<DateOnly>(type: "date", nullable: false),
                    EsercizioAnno = table.Column<int>(type: "int", nullable: false),
                    Descrizione = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Numero = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    CausaleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AnagraficaId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Stato = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Note = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MovimentiPrimaNota", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MovimentiPrimaNota_Anagrafiche_AnagraficaId",
                        column: x => x.AnagraficaId,
                        principalSchema: "app",
                        principalTable: "Anagrafiche",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MovimentiPrimaNota_Causali_CausaleId",
                        column: x => x.CausaleId,
                        principalSchema: "app",
                        principalTable: "Causali",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MovimentiPrimaNota_Esercizi_EsercizioAnno",
                        column: x => x.EsercizioAnno,
                        principalSchema: "app",
                        principalTable: "Esercizi",
                        principalColumn: "Anno",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AllegatiMovimento",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MovimentoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NomeFile = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    MimeType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Size = table.Column<long>(type: "bigint", nullable: false),
                    HashSha256 = table.Column<string>(type: "nchar(64)", fixedLength: true, maxLength: 64, nullable: false),
                    PathRelativo = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    UploadedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UploadedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AllegatiMovimento", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AllegatiMovimento_MovimentiPrimaNota_MovimentoId",
                        column: x => x.MovimentoId,
                        principalSchema: "app",
                        principalTable: "MovimentiPrimaNota",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RigheMovimento",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MovimentoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Importo = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ContoFinanziarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CategoriaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AnagraficaId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AliquotaIvaId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RigheMovimento", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RigheMovimento_AliquoteIva_AliquotaIvaId",
                        column: x => x.AliquotaIvaId,
                        principalSchema: "app",
                        principalTable: "AliquoteIva",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RigheMovimento_Anagrafiche_AnagraficaId",
                        column: x => x.AnagraficaId,
                        principalSchema: "app",
                        principalTable: "Anagrafiche",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RigheMovimento_Categorie_CategoriaId",
                        column: x => x.CategoriaId,
                        principalSchema: "app",
                        principalTable: "Categorie",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RigheMovimento_ContiFinanziari_ContoFinanziarioId",
                        column: x => x.ContoFinanziarioId,
                        principalSchema: "app",
                        principalTable: "ContiFinanziari",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RigheMovimento_MovimentiPrimaNota_MovimentoId",
                        column: x => x.MovimentoId,
                        principalSchema: "app",
                        principalTable: "MovimentiPrimaNota",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AllegatiMovimento_HashSha256",
                schema: "app",
                table: "AllegatiMovimento",
                column: "HashSha256");

            migrationBuilder.CreateIndex(
                name: "IX_AllegatiMovimento_MovimentoId",
                schema: "app",
                table: "AllegatiMovimento",
                column: "MovimentoId");

            migrationBuilder.CreateIndex(
                name: "IX_MovimentiPrimaNota_AnagraficaId",
                schema: "app",
                table: "MovimentiPrimaNota",
                column: "AnagraficaId");

            migrationBuilder.CreateIndex(
                name: "IX_MovimentiPrimaNota_CausaleId",
                schema: "app",
                table: "MovimentiPrimaNota",
                column: "CausaleId");

            migrationBuilder.CreateIndex(
                name: "IX_MovimentiPrimaNota_EsercizioAnno",
                schema: "app",
                table: "MovimentiPrimaNota",
                column: "EsercizioAnno");

            migrationBuilder.CreateIndex(
                name: "IX_MovimentiPrimaNota_EsercizioAnno_Data",
                schema: "app",
                table: "MovimentiPrimaNota",
                columns: new[] { "EsercizioAnno", "Data" });

            migrationBuilder.CreateIndex(
                name: "IX_MovimentiPrimaNota_Stato",
                schema: "app",
                table: "MovimentiPrimaNota",
                column: "Stato");

            migrationBuilder.CreateIndex(
                name: "IX_RigheMovimento_AliquotaIvaId",
                schema: "app",
                table: "RigheMovimento",
                column: "AliquotaIvaId");

            migrationBuilder.CreateIndex(
                name: "IX_RigheMovimento_AnagraficaId",
                schema: "app",
                table: "RigheMovimento",
                column: "AnagraficaId");

            migrationBuilder.CreateIndex(
                name: "IX_RigheMovimento_CategoriaId",
                schema: "app",
                table: "RigheMovimento",
                column: "CategoriaId");

            migrationBuilder.CreateIndex(
                name: "IX_RigheMovimento_ContoFinanziarioId",
                schema: "app",
                table: "RigheMovimento",
                column: "ContoFinanziarioId");

            migrationBuilder.CreateIndex(
                name: "IX_RigheMovimento_MovimentoId",
                schema: "app",
                table: "RigheMovimento",
                column: "MovimentoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AllegatiMovimento",
                schema: "app");

            migrationBuilder.DropTable(
                name: "RigheMovimento",
                schema: "app");

            migrationBuilder.DropTable(
                name: "MovimentiPrimaNota",
                schema: "app");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_Esercizi_Anno",
                schema: "app",
                table: "Esercizi");
        }
    }
}
