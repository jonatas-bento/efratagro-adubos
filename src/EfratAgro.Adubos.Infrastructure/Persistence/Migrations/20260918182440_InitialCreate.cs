using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EfratAgro.Adubos.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "legacy_import_batches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    SourceFileName = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    FinishedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ImportedRows = table.Column<int>(type: "int", nullable: false),
                    ReviewRows = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_legacy_import_batches", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "suppliers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    Name = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false),
                    NormalizedName = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_suppliers", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "warehouses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    Name = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_warehouses", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "legacy_import_rows",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    BatchId = table.Column<Guid>(type: "char(36)", nullable: false),
                    SheetName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    RowNumber = table.Column<int>(type: "int", nullable: false),
                    RawData = table.Column<string>(type: "longtext", nullable: false),
                    RequiresReview = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ReviewReason = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_legacy_import_rows", x => x.Id);
                    table.ForeignKey(
                        name: "FK_legacy_import_rows_legacy_import_batches_BatchId",
                        column: x => x.BatchId,
                        principalTable: "legacy_import_batches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "products",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    Name = table.Column<string>(type: "varchar(180)", maxLength: 180, nullable: false),
                    NormalizedName = table.Column<string>(type: "varchar(180)", maxLength: 180, nullable: false),
                    LegacyName = table.Column<string>(type: "varchar(180)", maxLength: 180, nullable: true),
                    SupplierId = table.Column<Guid>(type: "char(36)", nullable: false),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_products", x => x.Id);
                    table.ForeignKey(
                        name: "FK_products_suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "inventory_movements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    ProductId = table.Column<Guid>(type: "char(36)", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "char(36)", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Bucket = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    OccurredAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ReferenceType = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: true),
                    ReferenceId = table.Column<Guid>(type: "char(36)", nullable: true),
                    LegacyImportRowId = table.Column<Guid>(type: "char(36)", nullable: true),
                    Notes = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inventory_movements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_inventory_movements_legacy_import_rows_LegacyImportRowId",
                        column: x => x.LegacyImportRowId,
                        principalTable: "legacy_import_rows",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_inventory_movements_products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_inventory_movements_warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_movements_LegacyImportRowId",
                table: "inventory_movements",
                column: "LegacyImportRowId");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_movements_ProductId_WarehouseId_OccurredAtUtc",
                table: "inventory_movements",
                columns: new[] { "ProductId", "WarehouseId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_inventory_movements_WarehouseId",
                table: "inventory_movements",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_legacy_import_rows_BatchId_SheetName_RowNumber",
                table: "legacy_import_rows",
                columns: new[] { "BatchId", "SheetName", "RowNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_products_SupplierId_NormalizedName",
                table: "products",
                columns: new[] { "SupplierId", "NormalizedName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_suppliers_NormalizedName",
                table: "suppliers",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_warehouses_Name",
                table: "warehouses",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "inventory_movements");

            migrationBuilder.DropTable(
                name: "legacy_import_rows");

            migrationBuilder.DropTable(
                name: "products");

            migrationBuilder.DropTable(
                name: "warehouses");

            migrationBuilder.DropTable(
                name: "legacy_import_batches");

            migrationBuilder.DropTable(
                name: "suppliers");
        }
    }
}
