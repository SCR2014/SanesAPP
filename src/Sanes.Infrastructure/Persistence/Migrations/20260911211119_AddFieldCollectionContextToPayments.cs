using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sanes.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFieldCollectionContextToPayments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CollectedByAppUserId",
                table: "payments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CollectionRouteId",
                table: "payments",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_payments_CollectedByAppUserId",
                table: "payments",
                column: "CollectedByAppUserId");

            migrationBuilder.CreateIndex(
                name: "IX_payments_CollectionRouteId",
                table: "payments",
                column: "CollectionRouteId");

            migrationBuilder.AddForeignKey(
                name: "FK_payments_AppUsers_CollectedByAppUserId",
                table: "payments",
                column: "CollectedByAppUserId",
                principalTable: "AppUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_payments_CollectionRoutes_CollectionRouteId",
                table: "payments",
                column: "CollectionRouteId",
                principalTable: "CollectionRoutes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_payments_AppUsers_CollectedByAppUserId",
                table: "payments");

            migrationBuilder.DropForeignKey(
                name: "FK_payments_CollectionRoutes_CollectionRouteId",
                table: "payments");

            migrationBuilder.DropIndex(
                name: "IX_payments_CollectedByAppUserId",
                table: "payments");

            migrationBuilder.DropIndex(
                name: "IX_payments_CollectionRouteId",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "CollectedByAppUserId",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "CollectionRouteId",
                table: "payments");
        }
    }
}
