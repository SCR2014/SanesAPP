using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sanes.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MakeInvestorIdentificationUnique : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_investors_TenantId_Identification",
                table: "investors");

            migrationBuilder.CreateIndex(
                name: "IX_investors_TenantId_Identification",
                table: "investors",
                columns: new[] { "TenantId", "Identification" },
                unique: true);
            
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_investors_TenantId_Identification",
                table: "investors");

            migrationBuilder.CreateIndex(
                name: "IX_investors_TenantId_Identification",
                table: "investors",
                columns: new[] { "TenantId", "Identification" });
        }
    }
}
