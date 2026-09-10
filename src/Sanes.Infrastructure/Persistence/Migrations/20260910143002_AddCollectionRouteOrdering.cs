using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sanes.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCollectionRouteOrdering : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "OrderMode",
                table: "CollectionRoutes",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "CollectionRouteOrder",
                table: "clients",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_clients_TenantId_CollectionRouteId_CollectionRouteOrder",
                table: "clients",
                columns: new[] { "TenantId", "CollectionRouteId", "CollectionRouteOrder" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_clients_TenantId_CollectionRouteId_CollectionRouteOrder",
                table: "clients");

            migrationBuilder.DropColumn(
                name: "OrderMode",
                table: "CollectionRoutes");

            migrationBuilder.DropColumn(
                name: "CollectionRouteOrder",
                table: "clients");
        }
    }
}
