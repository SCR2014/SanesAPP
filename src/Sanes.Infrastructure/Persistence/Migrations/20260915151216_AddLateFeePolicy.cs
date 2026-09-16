using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sanes.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLateFeePolicy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "DefaultLateFeeAmount",
                table: "tenants",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "DefaultLateFeeCalculationType",
                table: "tenants",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<bool>(
                name: "DefaultLateFeeEnabled",
                table: "tenants",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "DefaultLateFeeGraceDays",
                table: "tenants",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "LateFeeAmount",
                table: "loans",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "LateFeeCalculationType",
                table: "loans",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<bool>(
                name: "LateFeeEnabled",
                table: "loans",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "LateFeeGraceDays",
                table: "loans",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DefaultLateFeeAmount",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "DefaultLateFeeCalculationType",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "DefaultLateFeeEnabled",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "DefaultLateFeeGraceDays",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "LateFeeAmount",
                table: "loans");

            migrationBuilder.DropColumn(
                name: "LateFeeCalculationType",
                table: "loans");

            migrationBuilder.DropColumn(
                name: "LateFeeEnabled",
                table: "loans");

            migrationBuilder.DropColumn(
                name: "LateFeeGraceDays",
                table: "loans");
        }
    }
}
