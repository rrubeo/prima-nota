using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrimaNota.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuditLog",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Kind = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    TargetType = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    TargetId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Summary = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    PayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CorrelationId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLog", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLog_Kind",
                schema: "app",
                table: "AuditLog",
                column: "Kind");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLog_OccurredAt",
                schema: "app",
                table: "AuditLog",
                column: "OccurredAt");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLog_UserId_OccurredAt",
                schema: "app",
                table: "AuditLog",
                columns: new[] { "UserId", "OccurredAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditLog",
                schema: "app");
        }
    }
}
