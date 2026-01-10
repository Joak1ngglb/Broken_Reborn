using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Intersect.Server.Migrations.Sqlite.Game
{
    /// <inheritdoc />
    public partial class Addeffectrunes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StatRange_Cures_HighRange",
                table: "Items_EquipmentProperties");

            migrationBuilder.DropColumn(
                name: "StatRange_Cures_LowRange",
                table: "Items_EquipmentProperties");

            migrationBuilder.DropColumn(
                name: "StatRange_Damages_HighRange",
                table: "Items_EquipmentProperties");

            migrationBuilder.DropColumn(
                name: "StatRange_Damages_LowRange",
                table: "Items_EquipmentProperties");

            migrationBuilder.AddColumn<byte>(
                name: "TargetEffect",
                table: "Items",
                type: "INTEGER",
                nullable: false,
                defaultValue: (byte)0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TargetEffect",
                table: "Items");

            migrationBuilder.AddColumn<int>(
                name: "StatRange_Cures_HighRange",
                table: "Items_EquipmentProperties",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StatRange_Cures_LowRange",
                table: "Items_EquipmentProperties",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StatRange_Damages_HighRange",
                table: "Items_EquipmentProperties",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StatRange_Damages_LowRange",
                table: "Items_EquipmentProperties",
                type: "INTEGER",
                nullable: true);
        }
    }
}
