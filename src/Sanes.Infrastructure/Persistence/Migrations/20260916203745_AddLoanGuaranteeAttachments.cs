using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sanes.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLoanGuaranteeAttachments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "loan_guarantee_attachments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    LoanGuaranteeId = table.Column<Guid>(type: "uuid", nullable: false),
                    UploadedByAppUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    OriginalFileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    StorageKey = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedByAppUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_loan_guarantee_attachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_loan_guarantee_attachments_AppUsers_DeletedByAppUserId",
                        column: x => x.DeletedByAppUserId,
                        principalTable: "AppUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_loan_guarantee_attachments_AppUsers_UploadedByAppUserId",
                        column: x => x.UploadedByAppUserId,
                        principalTable: "AppUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_loan_guarantee_attachments_loan_guarantees_LoanGuaranteeId",
                        column: x => x.LoanGuaranteeId,
                        principalTable: "loan_guarantees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_loan_guarantee_attachments_tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_loan_guarantee_attachments_DeletedByAppUserId",
                table: "loan_guarantee_attachments",
                column: "DeletedByAppUserId");

            migrationBuilder.CreateIndex(
                name: "IX_loan_guarantee_attachments_LoanGuaranteeId",
                table: "loan_guarantee_attachments",
                column: "LoanGuaranteeId");

            migrationBuilder.CreateIndex(
                name: "IX_loan_guarantee_attachments_StorageKey",
                table: "loan_guarantee_attachments",
                column: "StorageKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_loan_guarantee_attachments_TenantId",
                table: "loan_guarantee_attachments",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_loan_guarantee_attachments_TenantId_LoanGuaranteeId",
                table: "loan_guarantee_attachments",
                columns: new[] { "TenantId", "LoanGuaranteeId" });

            migrationBuilder.CreateIndex(
                name: "IX_loan_guarantee_attachments_TenantId_LoanGuaranteeId_IsDelet~",
                table: "loan_guarantee_attachments",
                columns: new[] { "TenantId", "LoanGuaranteeId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_loan_guarantee_attachments_UploadedByAppUserId",
                table: "loan_guarantee_attachments",
                column: "UploadedByAppUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "loan_guarantee_attachments");
        }
    }
}
