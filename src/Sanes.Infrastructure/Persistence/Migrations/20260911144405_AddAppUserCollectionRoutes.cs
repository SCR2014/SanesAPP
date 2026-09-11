using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sanes.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAppUserCollectionRoutes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppUserCollectionRoutes",
                columns: table => new
                {
                    AppUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CollectionRouteId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppUserCollectionRoutes", x => new { x.AppUserId, x.CollectionRouteId });
                    table.ForeignKey(
                        name: "FK_AppUserCollectionRoutes_AppUsers_AppUserId",
                        column: x => x.AppUserId,
                        principalTable: "AppUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AppUserCollectionRoutes_CollectionRoutes_CollectionRouteId",
                        column: x => x.CollectionRouteId,
                        principalTable: "CollectionRoutes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppUserCollectionRoutes_CollectionRouteId",
                table: "AppUserCollectionRoutes",
                column: "CollectionRouteId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppUserCollectionRoutes");
        }
    }
}
