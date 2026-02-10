using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Intersect.Server.Migrations.Sqlite.Player
{
    /// <inheritdoc />
    public partial class ActivePlayerShopUniqueness : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ActiveUniquenessToken",
                table: "Player_Shops",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LiquidatedAt",
                table: "Player_Shops",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "LiquidatedGold",
                table: "Player_Shops",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateIndex(
                name: "IX_Player_Shops_MapId_MapInstanceId_X_Y_Z_ActiveUniquenessToken",
                table: "Player_Shops",
                columns: new[] { "MapId", "MapInstanceId", "X", "Y", "Z", "ActiveUniquenessToken" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Player_Shops_OwnerId_ActiveUniquenessToken",
                table: "Player_Shops",
                columns: new[] { "OwnerId", "ActiveUniquenessToken" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Player_Shops_MapId_MapInstanceId_X_Y_Z_ActiveUniquenessToken",
                table: "Player_Shops");

            migrationBuilder.DropIndex(
                name: "IX_Player_Shops_OwnerId_ActiveUniquenessToken",
                table: "Player_Shops");

            migrationBuilder.DropColumn(
                name: "ActiveUniquenessToken",
                table: "Player_Shops");

            migrationBuilder.DropColumn(
                name: "LiquidatedAt",
                table: "Player_Shops");

            migrationBuilder.DropColumn(
                name: "LiquidatedGold",
                table: "Player_Shops");
        }
    }
}
