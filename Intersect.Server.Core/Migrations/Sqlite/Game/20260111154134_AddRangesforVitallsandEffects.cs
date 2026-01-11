using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Intersect.Server.Migrations.Sqlite.Game
{
    /// <inheritdoc />
    public partial class AddRangesforVitallsandEffects : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EffectRange_Accuracy_HighRange",
                table: "Items_EquipmentProperties",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EffectRange_Accuracy_LowRange",
                table: "Items_EquipmentProperties",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EffectRange_AntiCritChance_HighRange",
                table: "Items_EquipmentProperties",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EffectRange_AntiCritChance_LowRange",
                table: "Items_EquipmentProperties",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EffectRange_ArmorPenetration_HighRange",
                table: "Items_EquipmentProperties",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EffectRange_ArmorPenetration_LowRange",
                table: "Items_EquipmentProperties",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EffectRange_CooldownReduction_HighRange",
                table: "Items_EquipmentProperties",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EffectRange_CooldownReduction_LowRange",
                table: "Items_EquipmentProperties",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EffectRange_CriticalChance_HighRange",
                table: "Items_EquipmentProperties",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EffectRange_CriticalChance_LowRange",
                table: "Items_EquipmentProperties",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EffectRange_Cures_HighRange",
                table: "Items_EquipmentProperties",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EffectRange_Cures_LowRange",
                table: "Items_EquipmentProperties",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EffectRange_DamageReduction_HighRange",
                table: "Items_EquipmentProperties",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EffectRange_DamageReduction_LowRange",
                table: "Items_EquipmentProperties",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EffectRange_DamageReflect_HighRange",
                table: "Items_EquipmentProperties",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EffectRange_DamageReflect_LowRange",
                table: "Items_EquipmentProperties",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EffectRange_Damages_HighRange",
                table: "Items_EquipmentProperties",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EffectRange_Damages_LowRange",
                table: "Items_EquipmentProperties",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EffectRange_EXP_HighRange",
                table: "Items_EquipmentProperties",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EffectRange_EXP_LowRange",
                table: "Items_EquipmentProperties",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EffectRange_Evasion_HighRange",
                table: "Items_EquipmentProperties",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EffectRange_Evasion_LowRange",
                table: "Items_EquipmentProperties",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EffectRange_Lifesteal_HighRange",
                table: "Items_EquipmentProperties",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EffectRange_Lifesteal_LowRange",
                table: "Items_EquipmentProperties",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EffectRange_Luck_HighRange",
                table: "Items_EquipmentProperties",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EffectRange_Luck_LowRange",
                table: "Items_EquipmentProperties",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EffectRange_Manasteal_HighRange",
                table: "Items_EquipmentProperties",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EffectRange_Manasteal_LowRange",
                table: "Items_EquipmentProperties",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EffectRange_Tenacity_HighRange",
                table: "Items_EquipmentProperties",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EffectRange_Tenacity_LowRange",
                table: "Items_EquipmentProperties",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VitalRange_Health_HighRange",
                table: "Items_EquipmentProperties",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VitalRange_Health_LowRange",
                table: "Items_EquipmentProperties",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VitalRange_Mana_HighRange",
                table: "Items_EquipmentProperties",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VitalRange_Mana_LowRange",
                table: "Items_EquipmentProperties",
                type: "INTEGER",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EffectRange_Accuracy_HighRange",
                table: "Items_EquipmentProperties");

            migrationBuilder.DropColumn(
                name: "EffectRange_Accuracy_LowRange",
                table: "Items_EquipmentProperties");

            migrationBuilder.DropColumn(
                name: "EffectRange_AntiCritChance_HighRange",
                table: "Items_EquipmentProperties");

            migrationBuilder.DropColumn(
                name: "EffectRange_AntiCritChance_LowRange",
                table: "Items_EquipmentProperties");

            migrationBuilder.DropColumn(
                name: "EffectRange_ArmorPenetration_HighRange",
                table: "Items_EquipmentProperties");

            migrationBuilder.DropColumn(
                name: "EffectRange_ArmorPenetration_LowRange",
                table: "Items_EquipmentProperties");

            migrationBuilder.DropColumn(
                name: "EffectRange_CooldownReduction_HighRange",
                table: "Items_EquipmentProperties");

            migrationBuilder.DropColumn(
                name: "EffectRange_CooldownReduction_LowRange",
                table: "Items_EquipmentProperties");

            migrationBuilder.DropColumn(
                name: "EffectRange_CriticalChance_HighRange",
                table: "Items_EquipmentProperties");

            migrationBuilder.DropColumn(
                name: "EffectRange_CriticalChance_LowRange",
                table: "Items_EquipmentProperties");

            migrationBuilder.DropColumn(
                name: "EffectRange_Cures_HighRange",
                table: "Items_EquipmentProperties");

            migrationBuilder.DropColumn(
                name: "EffectRange_Cures_LowRange",
                table: "Items_EquipmentProperties");

            migrationBuilder.DropColumn(
                name: "EffectRange_DamageReduction_HighRange",
                table: "Items_EquipmentProperties");

            migrationBuilder.DropColumn(
                name: "EffectRange_DamageReduction_LowRange",
                table: "Items_EquipmentProperties");

            migrationBuilder.DropColumn(
                name: "EffectRange_DamageReflect_HighRange",
                table: "Items_EquipmentProperties");

            migrationBuilder.DropColumn(
                name: "EffectRange_DamageReflect_LowRange",
                table: "Items_EquipmentProperties");

            migrationBuilder.DropColumn(
                name: "EffectRange_Damages_HighRange",
                table: "Items_EquipmentProperties");

            migrationBuilder.DropColumn(
                name: "EffectRange_Damages_LowRange",
                table: "Items_EquipmentProperties");

            migrationBuilder.DropColumn(
                name: "EffectRange_EXP_HighRange",
                table: "Items_EquipmentProperties");

            migrationBuilder.DropColumn(
                name: "EffectRange_EXP_LowRange",
                table: "Items_EquipmentProperties");

            migrationBuilder.DropColumn(
                name: "EffectRange_Evasion_HighRange",
                table: "Items_EquipmentProperties");

            migrationBuilder.DropColumn(
                name: "EffectRange_Evasion_LowRange",
                table: "Items_EquipmentProperties");

            migrationBuilder.DropColumn(
                name: "EffectRange_Lifesteal_HighRange",
                table: "Items_EquipmentProperties");

            migrationBuilder.DropColumn(
                name: "EffectRange_Lifesteal_LowRange",
                table: "Items_EquipmentProperties");

            migrationBuilder.DropColumn(
                name: "EffectRange_Luck_HighRange",
                table: "Items_EquipmentProperties");

            migrationBuilder.DropColumn(
                name: "EffectRange_Luck_LowRange",
                table: "Items_EquipmentProperties");

            migrationBuilder.DropColumn(
                name: "EffectRange_Manasteal_HighRange",
                table: "Items_EquipmentProperties");

            migrationBuilder.DropColumn(
                name: "EffectRange_Manasteal_LowRange",
                table: "Items_EquipmentProperties");

            migrationBuilder.DropColumn(
                name: "EffectRange_Tenacity_HighRange",
                table: "Items_EquipmentProperties");

            migrationBuilder.DropColumn(
                name: "EffectRange_Tenacity_LowRange",
                table: "Items_EquipmentProperties");

            migrationBuilder.DropColumn(
                name: "VitalRange_Health_HighRange",
                table: "Items_EquipmentProperties");

            migrationBuilder.DropColumn(
                name: "VitalRange_Health_LowRange",
                table: "Items_EquipmentProperties");

            migrationBuilder.DropColumn(
                name: "VitalRange_Mana_HighRange",
                table: "Items_EquipmentProperties");

            migrationBuilder.DropColumn(
                name: "VitalRange_Mana_LowRange",
                table: "Items_EquipmentProperties");
        }
    }
}
