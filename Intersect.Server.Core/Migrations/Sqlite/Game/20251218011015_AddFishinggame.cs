using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Intersect.Server.Migrations.Sqlite.Game
{
    /// <inheritdoc />
    public partial class AddFishinggame : Migration
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

            migrationBuilder.CreateTable(
                name: "Fish",
                columns: table => new
                {
                    Name = table.Column<string>(type: "TEXT", nullable: true),
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    chance = table.Column<int>(type: "INTEGER", nullable: false),
                    weight = table.Column<int>(type: "INTEGER", nullable: false),
                    pushStrength = table.Column<int>(type: "INTEGER", nullable: false),
                    strength = table.Column<int>(type: "INTEGER", nullable: false),
                    position = table.Column<int>(type: "INTEGER", nullable: false),
                    rangeSize = table.Column<int>(type: "INTEGER", nullable: false),
                    speedMove = table.Column<int>(type: "INTEGER", nullable: false),
                    speedChangeRangeSize = table.Column<int>(type: "INTEGER", nullable: false),
                    coeffUnpredictability = table.Column<int>(type: "INTEGER", nullable: false),
                    timeChangeSpeed = table.Column<int>(type: "INTEGER", nullable: false),
                    timeChangeRangeSize = table.Column<int>(type: "INTEGER", nullable: false),
                    ItemId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Folder = table.Column<string>(type: "TEXT", nullable: true),
                    Event = table.Column<Guid>(type: "TEXT", nullable: false),
                    FishingRequirements = table.Column<string>(type: "TEXT", nullable: true),
                    TimeCreated = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Fish", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FishingSpots",
                columns: table => new
                {
                    Name = table.Column<string>(type: "TEXT", nullable: true),
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Fishes = table.Column<string>(type: "TEXT", nullable: true),
                    FishingTimeMin = table.Column<int>(type: "INTEGER", nullable: false),
                    FishingTimeMax = table.Column<int>(type: "INTEGER", nullable: false),
                    Folder = table.Column<string>(type: "TEXT", nullable: true),
                    FishingRequirements = table.Column<string>(type: "TEXT", nullable: true),
                    TimeCreated = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FishingSpots", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Fish");

            migrationBuilder.DropTable(
                name: "FishingSpots");

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
