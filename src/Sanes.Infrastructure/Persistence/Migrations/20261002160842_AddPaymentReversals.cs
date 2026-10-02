using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sanes.Infrastructure.Persistence.Migrations
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
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    PaymentId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReversedByAppUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payment_reversals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_payment_reversals_AppUsers_ReversedByAppUserId",
                        column: x => x.ReversedByAppUserId,
                        principalTable: "AppUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_payment_reversals_payments_PaymentId",
                        column: x => x.PaymentId,
                        principalTable: "payments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_payment_reversals_tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_payment_reversals_PaymentId",
                table: "payment_reversals",
                column: "PaymentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_payment_reversals_ReversedByAppUserId",
                table: "payment_reversals",
                column: "ReversedByAppUserId");

            migrationBuilder.CreateIndex(
                name: "IX_payment_reversals_TenantId",
                table: "payment_reversals",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_payment_reversals_TenantId_CreatedAt",
                table: "payment_reversals",
                columns: new[] { "TenantId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_payment_reversals_TenantId_PaymentId",
                table: "payment_reversals",
                columns: new[] { "TenantId", "PaymentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_payment_reversals_TenantId_ReversedByAppUserId",
                table: "payment_reversals",
                columns: new[] { "TenantId", "ReversedByAppUserId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "payment_reversals");
        }
    }
}
