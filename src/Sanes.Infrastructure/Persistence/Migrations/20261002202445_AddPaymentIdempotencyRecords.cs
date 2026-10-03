using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sanes.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentIdempotencyRecords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "payment_idempotency_records",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    IdempotencyKey = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    PaymentId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payment_idempotency_records", x => x.Id);
                    table.ForeignKey(
                        name: "FK_payment_idempotency_records_payments_PaymentId",
                        column: x => x.PaymentId,
                        principalTable: "payments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_payment_idempotency_records_tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_payment_idempotency_records_PaymentId",
                table: "payment_idempotency_records",
                column: "PaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_payment_idempotency_records_TenantId_CreatedAt",
                table: "payment_idempotency_records",
                columns: new[] { "TenantId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_payment_idempotency_records_TenantId_IdempotencyKey",
                table: "payment_idempotency_records",
                columns: new[] { "TenantId", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_payment_idempotency_records_TenantId_PaymentId",
                table: "payment_idempotency_records",
                columns: new[] { "TenantId", "PaymentId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "payment_idempotency_records");
        }
    }
}
