using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sanes.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLateFeeLedgerAndPaymentAllocations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "late_fee_charges",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    LoanId = table.Column<Guid>(type: "uuid", nullable: false),
                    InstallmentNumber = table.Column<int>(type: "integer", nullable: false),
                    InstallmentDueDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EffectiveDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CalculationType = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_late_fee_charges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_late_fee_charges_loans_LoanId",
                        column: x => x.LoanId,
                        principalTable: "loans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_late_fee_charges_tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "late_fee_adjustments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    LateFeeChargeId = table.Column<Guid>(type: "uuid", nullable: false),
                    AppUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    AdjustmentType = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_late_fee_adjustments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_late_fee_adjustments_AppUsers_AppUserId",
                        column: x => x.AppUserId,
                        principalTable: "AppUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_late_fee_adjustments_late_fee_charges_LateFeeChargeId",
                        column: x => x.LateFeeChargeId,
                        principalTable: "late_fee_charges",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_late_fee_adjustments_tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "payment_allocations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    PaymentId = table.Column<Guid>(type: "uuid", nullable: false),
                    AllocationType = table.Column<int>(type: "integer", nullable: false),
                    LateFeeChargeId = table.Column<Guid>(type: "uuid", nullable: true),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payment_allocations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_payment_allocations_late_fee_charges_LateFeeChargeId",
                        column: x => x.LateFeeChargeId,
                        principalTable: "late_fee_charges",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_payment_allocations_payments_PaymentId",
                        column: x => x.PaymentId,
                        principalTable: "payments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_payment_allocations_tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.Sql(
                """
                INSERT INTO payment_allocations
                (
                    "Id",
                    "TenantId",
                    "PaymentId",
                    "AllocationType",
                    "LateFeeChargeId",
                    "Amount",
                    "CreatedAt"
                )
                SELECT
                    p."Id",
                    p."TenantId",
                    p."Id",
                    1,
                    NULL,
                    p."Amount",
                    p."CreatedAt"
                FROM payments AS p;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_late_fee_adjustments_AppUserId",
                table: "late_fee_adjustments",
                column: "AppUserId");

            migrationBuilder.CreateIndex(
                name: "IX_late_fee_adjustments_LateFeeChargeId",
                table: "late_fee_adjustments",
                column: "LateFeeChargeId");

            migrationBuilder.CreateIndex(
                name: "IX_late_fee_adjustments_TenantId",
                table: "late_fee_adjustments",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_late_fee_adjustments_TenantId_AppUserId",
                table: "late_fee_adjustments",
                columns: new[] { "TenantId", "AppUserId" });

            migrationBuilder.CreateIndex(
                name: "IX_late_fee_adjustments_TenantId_LateFeeChargeId",
                table: "late_fee_adjustments",
                columns: new[] { "TenantId", "LateFeeChargeId" });

            migrationBuilder.CreateIndex(
                name: "IX_late_fee_charges_LoanId",
                table: "late_fee_charges",
                column: "LoanId");

            migrationBuilder.CreateIndex(
                name: "IX_late_fee_charges_TenantId",
                table: "late_fee_charges",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_late_fee_charges_TenantId_LoanId",
                table: "late_fee_charges",
                columns: new[] { "TenantId", "LoanId" });

            migrationBuilder.CreateIndex(
                name: "IX_late_fee_charges_TenantId_LoanId_InstallmentNumber",
                table: "late_fee_charges",
                columns: new[] { "TenantId", "LoanId", "InstallmentNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_late_fee_charges_TenantId_LoanId_InstallmentNumber_Effectiv~",
                table: "late_fee_charges",
                columns: new[] { "TenantId", "LoanId", "InstallmentNumber", "EffectiveDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_payment_allocations_LateFeeChargeId",
                table: "payment_allocations",
                column: "LateFeeChargeId");

            migrationBuilder.CreateIndex(
                name: "IX_payment_allocations_PaymentId",
                table: "payment_allocations",
                column: "PaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_payment_allocations_TenantId",
                table: "payment_allocations",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_payment_allocations_TenantId_LateFeeChargeId",
                table: "payment_allocations",
                columns: new[] { "TenantId", "LateFeeChargeId" });

            migrationBuilder.CreateIndex(
                name: "IX_payment_allocations_TenantId_PaymentId",
                table: "payment_allocations",
                columns: new[] { "TenantId", "PaymentId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "late_fee_adjustments");

            migrationBuilder.DropTable(
                name: "payment_allocations");

            migrationBuilder.DropTable(
                name: "late_fee_charges");
        }
    }
}
