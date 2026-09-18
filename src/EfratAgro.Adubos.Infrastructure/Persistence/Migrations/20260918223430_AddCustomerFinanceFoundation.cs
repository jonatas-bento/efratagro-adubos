using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EfratAgro.Adubos.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerFinanceFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Origin",
                table: "sales",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateTable(
                name: "receivables",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    SaleId = table.Column<Guid>(type: "char(36)", nullable: false),
                    InstallmentNumber = table.Column<int>(type: "int", nullable: false),
                    DueDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    OriginalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Notes = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_receivables", x => x.Id);
                    table.ForeignKey(
                        name: "FK_receivables_sales_SaleId",
                        column: x => x.SaleId,
                        principalTable: "sales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "payments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    ReceivableId = table.Column<Guid>(type: "char(36)", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PaidAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Method = table.Column<int>(type: "int", nullable: false),
                    Reference = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: true),
                    Notes = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_payments_receivables_ReceivableId",
                        column: x => x.ReceivableId,
                        principalTable: "receivables",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_payments_PaidAtUtc",
                table: "payments",
                column: "PaidAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_payments_ReceivableId",
                table: "payments",
                column: "ReceivableId");

            migrationBuilder.CreateIndex(
                name: "IX_receivables_DueDate",
                table: "receivables",
                column: "DueDate");

            migrationBuilder.CreateIndex(
                name: "IX_receivables_SaleId",
                table: "receivables",
                column: "SaleId");

            migrationBuilder.CreateIndex(
                name: "IX_receivables_SaleId_InstallmentNumber",
                table: "receivables",
                columns: new[] { "SaleId", "InstallmentNumber" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "payments");

            migrationBuilder.DropTable(
                name: "receivables");

            migrationBuilder.DropColumn(
                name: "Origin",
                table: "sales");
        }
    }
}
