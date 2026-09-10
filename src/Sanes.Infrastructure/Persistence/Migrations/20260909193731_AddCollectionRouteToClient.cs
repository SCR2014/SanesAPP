using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sanes.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCollectionRouteToClient : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CollectionRouteId",
                table: "clients",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_clients_CollectionRouteId",
                table: "clients",
                column: "CollectionRouteId");

            migrationBuilder.AddForeignKey(
                name: "FK_clients_CollectionRoutes_CollectionRouteId",
                table: "clients",
                column: "CollectionRouteId",
                principalTable: "CollectionRoutes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_clients_CollectionRoutes_CollectionRouteId",
                table: "clients");

            migrationBuilder.DropIndex(
                name: "IX_clients_CollectionRouteId",
                table: "clients");

            migrationBuilder.DropColumn(
                name: "CollectionRouteId",
                table: "clients");
        }
    }
}
