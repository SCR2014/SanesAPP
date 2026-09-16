using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sanes.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLoanGuarantees : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "GuaranteeRequiredFromAmount",
                table: "tenants",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "GuaranteeRequired",
                table: "loans",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "GuaranteeThresholdAtCreation",
                table: "loans",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "loan_guarantees",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    LoanId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Reference = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_loan_guarantees", x => x.Id);
                    table.ForeignKey(
                        name: "FK_loan_guarantees_loans_LoanId",
                        column: x => x.LoanId,
                        principalTable: "loans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_loan_guarantees_tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_loan_guarantees_LoanId",
                table: "loan_guarantees",
                column: "LoanId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_loan_guarantees_TenantId",
                table: "loan_guarantees",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_loan_guarantees_TenantId_LoanId",
                table: "loan_guarantees",
                columns: new[] { "TenantId", "LoanId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "loan_guarantees");

            migrationBuilder.DropColumn(
                name: "GuaranteeRequiredFromAmount",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "GuaranteeRequired",
                table: "loans");

            migrationBuilder.DropColumn(
                name: "GuaranteeThresholdAtCreation",
                table: "loans");
        }
    }
}
