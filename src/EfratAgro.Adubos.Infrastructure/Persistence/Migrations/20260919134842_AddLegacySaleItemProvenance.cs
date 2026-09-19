using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EfratAgro.Adubos.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLegacySaleItemProvenance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SaleItemId",
                table: "legacy_import_rows",
                type: "char(36)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_legacy_import_rows_SaleItemId",
                table: "legacy_import_rows",
                column: "SaleItemId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_legacy_import_rows_sale_items_SaleItemId",
                table: "legacy_import_rows",
                column: "SaleItemId",
                principalTable: "sale_items",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_legacy_import_rows_sale_items_SaleItemId",
                table: "legacy_import_rows");

            migrationBuilder.DropIndex(
                name: "IX_legacy_import_rows_SaleItemId",
                table: "legacy_import_rows");

            migrationBuilder.DropColumn(
                name: "SaleItemId",
                table: "legacy_import_rows");
        }
    }
}
