using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Intersect.Server.Migrations.MySql.Game
{
    /// <inheritdoc />
    public partial class CombatRebalanceStats : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EffectRange_CriticalReduction_HighRange",
                table: "Items_EquipmentProperties",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EffectRange_CriticalReduction_LowRange",
                table: "Items_EquipmentProperties",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StatRange_Willpower_HighRange",
                table: "Items_EquipmentProperties",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StatRange_Willpower_LowRange",
                table: "Items_EquipmentProperties",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EffectRange_CriticalReduction_HighRange",
                table: "Items_EquipmentProperties");

            migrationBuilder.DropColumn(
                name: "EffectRange_CriticalReduction_LowRange",
                table: "Items_EquipmentProperties");

            migrationBuilder.DropColumn(
                name: "StatRange_Willpower_HighRange",
                table: "Items_EquipmentProperties");

            migrationBuilder.DropColumn(
                name: "StatRange_Willpower_LowRange",
                table: "Items_EquipmentProperties");
        }
    }
}
