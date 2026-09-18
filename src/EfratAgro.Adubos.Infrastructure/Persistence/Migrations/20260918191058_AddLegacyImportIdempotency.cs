using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EfratAgro.Adubos.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLegacyImportIdempotency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SourceFileHash",
                table: "legacy_import_batches",
                type: "varchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_legacy_import_batches_SourceFileHash",
                table: "legacy_import_batches",
                column: "SourceFileHash",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_legacy_import_batches_SourceFileHash",
                table: "legacy_import_batches");

            migrationBuilder.DropColumn(
                name: "SourceFileHash",
                table: "legacy_import_batches");
        }
    }
}
