using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sanes.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEarlySettlement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "loan_balance_adjustments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    LoanId = table.Column<Guid>(type: "uuid", nullable: false),
                    AppUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    AdjustmentType = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_loan_balance_adjustments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_loan_balance_adjustments_AppUsers_AppUserId",
                        column: x => x.AppUserId,
                        principalTable: "AppUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_loan_balance_adjustments_loans_LoanId",
                        column: x => x.LoanId,
                        principalTable: "loans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_loan_balance_adjustments_tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "early_settlements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    LoanId = table.Column<Guid>(type: "uuid", nullable: false),
                    AppUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    PaymentId = table.Column<Guid>(type: "uuid", nullable: true),
                    LoanBalanceAdjustmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    DiscountType = table.Column<int>(type: "integer", nullable: false),
                    DiscountValue = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CompletedInstallments = table.Column<int>(type: "integer", nullable: false),
                    ContractualBalanceBefore = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    LateFeeBalanceBefore = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalOutstandingBefore = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    SettlementAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_early_settlements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_early_settlements_AppUsers_AppUserId",
                        column: x => x.AppUserId,
                        principalTable: "AppUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_early_settlements_loan_balance_adjustments_LoanBalanceAdjus~",
                        column: x => x.LoanBalanceAdjustmentId,
                        principalTable: "loan_balance_adjustments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_early_settlements_loans_LoanId",
                        column: x => x.LoanId,
                        principalTable: "loans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_early_settlements_payments_PaymentId",
                        column: x => x.PaymentId,
                        principalTable: "payments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_early_settlements_tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_early_settlements_AppUserId",
                table: "early_settlements",
                column: "AppUserId");

            migrationBuilder.CreateIndex(
                name: "IX_early_settlements_LoanBalanceAdjustmentId",
                table: "early_settlements",
                column: "LoanBalanceAdjustmentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_early_settlements_LoanId",
                table: "early_settlements",
                column: "LoanId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_early_settlements_PaymentId",
                table: "early_settlements",
                column: "PaymentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_early_settlements_TenantId",
                table: "early_settlements",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_early_settlements_TenantId_AppUserId",
                table: "early_settlements",
                columns: new[] { "TenantId", "AppUserId" });

            migrationBuilder.CreateIndex(
                name: "IX_early_settlements_TenantId_LoanId",
                table: "early_settlements",
                columns: new[] { "TenantId", "LoanId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_loan_balance_adjustments_AppUserId",
                table: "loan_balance_adjustments",
                column: "AppUserId");

            migrationBuilder.CreateIndex(
                name: "IX_loan_balance_adjustments_LoanId",
                table: "loan_balance_adjustments",
                column: "LoanId");

            migrationBuilder.CreateIndex(
                name: "IX_loan_balance_adjustments_TenantId",
                table: "loan_balance_adjustments",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_loan_balance_adjustments_TenantId_AppUserId",
                table: "loan_balance_adjustments",
                columns: new[] { "TenantId", "AppUserId" });

            migrationBuilder.CreateIndex(
                name: "IX_loan_balance_adjustments_TenantId_LoanId",
                table: "loan_balance_adjustments",
                columns: new[] { "TenantId", "LoanId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "early_settlements");

            migrationBuilder.DropTable(
                name: "loan_balance_adjustments");
        }
    }
}
