using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sanes.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CreateCollectionRoute : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CollectionRoutes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CollectionRoutes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CollectionRoutes_tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CollectionRoutes_TenantId",
                table: "CollectionRoutes",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_CollectionRoutes_TenantId_Name",
                table: "CollectionRoutes",
                columns: new[] { "TenantId", "Name" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CollectionRoutes");
        }
    }
}
