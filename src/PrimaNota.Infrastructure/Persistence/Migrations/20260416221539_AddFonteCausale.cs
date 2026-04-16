using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrimaNota.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFonteCausale : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Fonte",
                schema: "app",
                table: "Causali",
                type: "nvarchar(16)",
                maxLength: 16,
                nullable: true);

            // Backfill existing seed causali so Vendite and Corrispettivi registers are split immediately.
            migrationBuilder.Sql("UPDATE [app].[Causali] SET [Fonte] = N'Fattura' WHERE [Codice] = N'INC-FATT';");
            migrationBuilder.Sql("UPDATE [app].[Causali] SET [Fonte] = N'Corrispettivo' WHERE [Codice] = N'INC-CASH';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Fonte",
                schema: "app",
                table: "Causali");
        }
    }
}
