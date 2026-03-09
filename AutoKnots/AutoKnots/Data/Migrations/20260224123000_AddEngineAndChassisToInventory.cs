using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoKnots.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEngineAndChassisToInventory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EngineNumber",
                table: "InventoryItems",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ChassisNumber",
                table: "InventoryItems",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EngineNumber",
                table: "InventoryItems");

            migrationBuilder.DropColumn(
                name: "ChassisNumber",
                table: "InventoryItems");
        }
    }
}

