using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EfratAgro.Adubos.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentReversals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "payment_reversals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    PaymentId = table.Column<Guid>(type: "char(36)", nullable: false),
                    ReversedByUserId = table.Column<Guid>(type: "char(36)", nullable: false),
                    Reason = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false),
                    ReversedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payment_reversals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_payment_reversals_payments_PaymentId",
                        column: x => x.PaymentId,
                        principalTable: "payments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_payment_reversals_PaymentId",
                table: "payment_reversals",
                column: "PaymentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_payment_reversals_ReversedAtUtc",
                table: "payment_reversals",
                column: "ReversedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_payment_reversals_ReversedByUserId",
                table: "payment_reversals",
                column: "ReversedByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "payment_reversals");
        }
    }
}
