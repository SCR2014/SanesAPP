using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sanes.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UpdateInvestorTenantRelation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Notes",
                table: "investors",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_investors_TenantId_Identification",
                table: "investors",
                columns: new[] { "TenantId", "Identification" });

            migrationBuilder.AddForeignKey(
                name: "FK_investors_tenants_TenantId",
                table: "investors",
                column: "TenantId",
                principalTable: "tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_investors_tenants_TenantId",
                table: "investors");

            migrationBuilder.DropIndex(
                name: "IX_investors_TenantId_Identification",
                table: "investors");

            migrationBuilder.AlterColumn<string>(
                name: "Notes",
                table: "investors",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(1000)",
                oldMaxLength: 1000,
                oldNullable: true);
        }
    }
}
