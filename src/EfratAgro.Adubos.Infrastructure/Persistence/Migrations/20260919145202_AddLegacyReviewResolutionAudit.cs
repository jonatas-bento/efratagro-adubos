using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EfratAgro.Adubos.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLegacyReviewResolutionAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ResolutionNote",
                table: "legacy_import_rows",
                type: "varchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ResolvedAtUtc",
                table: "legacy_import_rows",
                type: "datetime(6)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ResolutionNote",
                table: "legacy_import_rows");

            migrationBuilder.DropColumn(
                name: "ResolvedAtUtc",
                table: "legacy_import_rows");
        }
    }
}
