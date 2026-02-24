using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoKnots.Data.Migrations
{
    /// <inheritdoc />
    public partial class InventoryCarsSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_InventoryItems_SKU",
                table: "InventoryItems");

            migrationBuilder.DropColumn(
                name: "SKU",
                table: "InventoryItems");

            migrationBuilder.AddColumn<string>(
                name: "Make",
                table: "InventoryItems",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Model",
                table: "InventoryItems",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "PurchaseDate",
                table: "InventoryItems",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Variant",
                table: "InventoryItems",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Make",
                table: "InventoryItems");

            migrationBuilder.DropColumn(
                name: "Model",
                table: "InventoryItems");

            migrationBuilder.DropColumn(
                name: "PurchaseDate",
                table: "InventoryItems");

            migrationBuilder.DropColumn(
                name: "Variant",
                table: "InventoryItems");

            migrationBuilder.AddColumn<string>(
                name: "SKU",
                table: "InventoryItems",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryItems_SKU",
                table: "InventoryItems",
                column: "SKU",
                unique: true);
        }
    }
}
