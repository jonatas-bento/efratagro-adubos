using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EfratAgro.Adubos.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLegacyCommercialImportFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_legacy_import_batches_SourceFileHash",
                table: "legacy_import_batches");

            migrationBuilder.AddColumn<int>(
                name: "Scope",
                table: "legacy_import_batches",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateTable(
                name: "legacy_sale_metadata",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    SaleId = table.Column<Guid>(type: "char(36)", nullable: false),
                    ImportBatchId = table.Column<Guid>(type: "char(36)", nullable: false),
                    TransactionKey = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false),
                    DocumentNumber = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false),
                    TransactionDate = table.Column<DateTime>(type: "date", nullable: false),
                    ImportedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_legacy_sale_metadata", x => x.Id);
                    table.ForeignKey(
                        name: "FK_legacy_sale_metadata_legacy_import_batches_ImportBatchId",
                        column: x => x.ImportBatchId,
                        principalTable: "legacy_import_batches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_legacy_sale_metadata_sales_SaleId",
                        column: x => x.SaleId,
                        principalTable: "sales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_legacy_import_batches_SourceFileHash_Scope",
                table: "legacy_import_batches",
                columns: new[] { "SourceFileHash", "Scope" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_legacy_sale_metadata_DocumentNumber",
                table: "legacy_sale_metadata",
                column: "DocumentNumber");

            migrationBuilder.CreateIndex(
                name: "IX_legacy_sale_metadata_ImportBatchId_TransactionKey",
                table: "legacy_sale_metadata",
                columns: new[] { "ImportBatchId", "TransactionKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_legacy_sale_metadata_SaleId",
                table: "legacy_sale_metadata",
                column: "SaleId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_legacy_sale_metadata_TransactionDate",
                table: "legacy_sale_metadata",
                column: "TransactionDate");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "legacy_sale_metadata");

            migrationBuilder.DropIndex(
                name: "IX_legacy_import_batches_SourceFileHash_Scope",
                table: "legacy_import_batches");

            migrationBuilder.DropColumn(
                name: "Scope",
                table: "legacy_import_batches");

            migrationBuilder.CreateIndex(
                name: "IX_legacy_import_batches_SourceFileHash",
                table: "legacy_import_batches",
                column: "SourceFileHash",
                unique: true);
        }
    }
}
