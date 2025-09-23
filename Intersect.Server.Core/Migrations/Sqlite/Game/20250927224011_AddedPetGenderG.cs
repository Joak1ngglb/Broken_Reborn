using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Intersect.Server.Migrations.Sqlite.Game
{
    /// <inheritdoc />
    public partial class AddedPetGenderG : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CanEvolve",
                table: "Pets");

            migrationBuilder.DropColumn(
                name: "EvolutionLevel",
                table: "Pets");

            migrationBuilder.RenameColumn(
                name: "EvolutionTarget",
                table: "Pets",
                newName: "FeedingItem");

            migrationBuilder.AddColumn<int>(
                name: "BaseEnergy",
                table: "Pets",
                type: "INTEGER",
                nullable: false,
                defaultValue: 100);

            migrationBuilder.AddColumn<int>(
                name: "BaseMaturity",
                table: "Pets",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "BaseMood",
                table: "Pets",
                type: "INTEGER",
                nullable: false,
                defaultValue: 100);

            migrationBuilder.AddColumn<string>(
                name: "FemaleSprite",
                table: "Pets",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MaleSprite",
                table: "Pets",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BaseEnergy",
                table: "Pets");

            migrationBuilder.DropColumn(
                name: "BaseMaturity",
                table: "Pets");

            migrationBuilder.DropColumn(
                name: "BaseMood",
                table: "Pets");

            migrationBuilder.DropColumn(
                name: "FemaleSprite",
                table: "Pets");

            migrationBuilder.DropColumn(
                name: "MaleSprite",
                table: "Pets");

            migrationBuilder.RenameColumn(
                name: "FeedingItem",
                table: "Pets",
                newName: "EvolutionTarget");

            migrationBuilder.AddColumn<bool>(
                name: "CanEvolve",
                table: "Pets",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "EvolutionLevel",
                table: "Pets",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }
    }
}
