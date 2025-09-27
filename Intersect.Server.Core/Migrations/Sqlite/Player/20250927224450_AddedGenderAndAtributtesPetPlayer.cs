using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Intersect.Server.Migrations.Sqlite.Player
{
    /// <inheritdoc />
    public partial class AddedGenderAndAtributtesPetPlayer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "CareMilliseconds",
                table: "Player_Pets",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<int>(
                name: "Energy",
                table: "Player_Pets",
                type: "INTEGER",
                nullable: false,
                defaultValue: -1);

            migrationBuilder.AddColumn<int>(
                name: "Gender",
                table: "Player_Pets",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<long>(
                name: "LastWhimFulfillmentTicks",
                table: "Player_Pets",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<int>(
                name: "Maturity",
                table: "Player_Pets",
                type: "INTEGER",
                nullable: false,
                defaultValue: -1);

            migrationBuilder.AddColumn<int>(
                name: "Mood",
                table: "Player_Pets",
                type: "INTEGER",
                nullable: false,
                defaultValue: -1);

            migrationBuilder.AddColumn<int>(
                name: "WhimsFulfilled",
                table: "Player_Pets",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CareMilliseconds",
                table: "Player_Pets");

            migrationBuilder.DropColumn(
                name: "Energy",
                table: "Player_Pets");

            migrationBuilder.DropColumn(
                name: "Gender",
                table: "Player_Pets");

            migrationBuilder.DropColumn(
                name: "LastWhimFulfillmentTicks",
                table: "Player_Pets");

            migrationBuilder.DropColumn(
                name: "Maturity",
                table: "Player_Pets");

            migrationBuilder.DropColumn(
                name: "Mood",
                table: "Player_Pets");

            migrationBuilder.DropColumn(
                name: "WhimsFulfilled",
                table: "Player_Pets");
        }
    }
}
