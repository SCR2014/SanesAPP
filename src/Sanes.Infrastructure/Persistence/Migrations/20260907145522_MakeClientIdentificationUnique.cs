using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sanes.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MakeClientIdentificationUnique : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_clients_TenantId_Identification",
                table: "clients");

            migrationBuilder.CreateIndex(
                name: "IX_clients_TenantId_Identification",
                table: "clients",
                columns: new[] { "TenantId", "Identification" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_clients_TenantId_Identification",
                table: "clients");

            migrationBuilder.CreateIndex(
                name: "IX_clients_TenantId_Identification",
                table: "clients",
                columns: new[] { "TenantId", "Identification" });
        }
    }
}
