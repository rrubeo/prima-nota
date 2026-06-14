using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrimaNota.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddIntegrazioneArubaAndIdSdi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "IdentificativoSdi",
                schema: "app",
                table: "MovimentiPrimaNota",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "IntegrazioneAruba",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    Abilitata = table.Column<bool>(type: "bit", nullable: false),
                    Username = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    PasswordProtetta = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    UsaDemo = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntegrazioneAruba", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MovimentiPrimaNota_IdentificativoSdi",
                schema: "app",
                table: "MovimentiPrimaNota",
                column: "IdentificativoSdi");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IntegrazioneAruba",
                schema: "app");

            migrationBuilder.DropIndex(
                name: "IX_MovimentiPrimaNota_IdentificativoSdi",
                schema: "app",
                table: "MovimentiPrimaNota");

            migrationBuilder.DropColumn(
                name: "IdentificativoSdi",
                schema: "app",
                table: "MovimentiPrimaNota");
        }
    }
}
