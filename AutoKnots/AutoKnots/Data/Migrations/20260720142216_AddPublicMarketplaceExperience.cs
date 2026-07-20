using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoKnots.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPublicMarketplaceExperience : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiresAt",
                table: "VehicleListings",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Features",
                table: "VehicleListings",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "InspectionStatus",
                table: "VehicleListings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "InspectionSummary",
                table: "VehicleListings",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsFeatured",
                table: "VehicleListings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ShowSellerName",
                table: "VehicleListings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Slug",
                table: "VehicleListings",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VehicleHistory",
                table: "VehicleListings",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.Sql("UPDATE VehicleListings SET Slug = CONCAT('vehicle-', Id) WHERE Slug IS NULL OR Slug = ''");

            migrationBuilder.AlterColumn<string>(
                name: "Slug",
                table: "VehicleListings",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_VehicleListings_Slug",
                table: "VehicleListings",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VehicleListings_Status_BodyStyle",
                table: "VehicleListings",
                columns: new[] { "Status", "BodyStyle" });

            migrationBuilder.CreateIndex(
                name: "IX_VehicleListings_Status_Condition",
                table: "VehicleListings",
                columns: new[] { "Status", "Condition" });

            migrationBuilder.CreateIndex(
                name: "IX_VehicleListings_Status_ExpiresAt",
                table: "VehicleListings",
                columns: new[] { "Status", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_VehicleListings_Status_FuelType",
                table: "VehicleListings",
                columns: new[] { "Status", "FuelType" });

            migrationBuilder.CreateIndex(
                name: "IX_VehicleListings_Status_Transmission",
                table: "VehicleListings",
                columns: new[] { "Status", "Transmission" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_VehicleListings_Slug",
                table: "VehicleListings");

            migrationBuilder.DropIndex(
                name: "IX_VehicleListings_Status_BodyStyle",
                table: "VehicleListings");

            migrationBuilder.DropIndex(
                name: "IX_VehicleListings_Status_Condition",
                table: "VehicleListings");

            migrationBuilder.DropIndex(
                name: "IX_VehicleListings_Status_ExpiresAt",
                table: "VehicleListings");

            migrationBuilder.DropIndex(
                name: "IX_VehicleListings_Status_FuelType",
                table: "VehicleListings");

            migrationBuilder.DropIndex(
                name: "IX_VehicleListings_Status_Transmission",
                table: "VehicleListings");

            migrationBuilder.DropColumn(
                name: "ExpiresAt",
                table: "VehicleListings");

            migrationBuilder.DropColumn(
                name: "Features",
                table: "VehicleListings");

            migrationBuilder.DropColumn(
                name: "InspectionStatus",
                table: "VehicleListings");

            migrationBuilder.DropColumn(
                name: "InspectionSummary",
                table: "VehicleListings");

            migrationBuilder.DropColumn(
                name: "IsFeatured",
                table: "VehicleListings");

            migrationBuilder.DropColumn(
                name: "ShowSellerName",
                table: "VehicleListings");

            migrationBuilder.DropColumn(
                name: "Slug",
                table: "VehicleListings");

            migrationBuilder.DropColumn(
                name: "VehicleHistory",
                table: "VehicleListings");
        }
    }
}
