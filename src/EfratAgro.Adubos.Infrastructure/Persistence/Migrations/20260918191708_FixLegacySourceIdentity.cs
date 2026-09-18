using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EfratAgro.Adubos.Infrastructure.Persistence.Migrations;

public partial class FixLegacySourceIdentity : Migration
{
    protected override void Up(
        MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "SourceCell",
            table: "legacy_import_rows",
            type: "varchar(20)",
            maxLength: 20,
            nullable: false,
            defaultValue: "");

        // IMPORTANT:
        // BatchId is a foreign key. MySQL requires an index
        // beginning with BatchId to exist at all times.
        //
        // Therefore the replacement index must be created
        // BEFORE the old index is removed.
        migrationBuilder.CreateIndex(
            name: "IX_legacy_import_rows_BatchId_SheetName_SourceCell",
            table: "legacy_import_rows",
            columns: new[]
            {
                "BatchId",
                "SheetName",
                "SourceCell"
            },
            unique: true);

        migrationBuilder.DropIndex(
            name: "IX_legacy_import_rows_BatchId_SheetName_RowNumber",
            table: "legacy_import_rows");
    }

    protected override void Down(
        MigrationBuilder migrationBuilder)
    {
        // Same rule in reverse:
        // create an index capable of supporting BatchId
        // before removing the current one.
        migrationBuilder.CreateIndex(
            name: "IX_legacy_import_rows_BatchId_SheetName_RowNumber",
            table: "legacy_import_rows",
            columns: new[]
            {
                "BatchId",
                "SheetName",
                "RowNumber"
            },
            unique: true);

        migrationBuilder.DropIndex(
            name: "IX_legacy_import_rows_BatchId_SheetName_SourceCell",
            table: "legacy_import_rows");

        migrationBuilder.DropColumn(
            name: "SourceCell",
            table: "legacy_import_rows");
    }
}
