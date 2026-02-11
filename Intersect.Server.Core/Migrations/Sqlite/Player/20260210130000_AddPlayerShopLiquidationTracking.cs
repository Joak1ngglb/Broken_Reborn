using System;
using Intersect.Server.Database.PlayerData;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Intersect.Server.Migrations.Sqlite.Player
{
    [DbContext(typeof(SqlitePlayerContext))]
    [Migration("20260210130000_AddPlayerShopLiquidationTracking")]
    public partial class AddPlayerShopLiquidationTracking : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            switch (migrationBuilder.ActiveProvider)
            {
                case "Microsoft.EntityFrameworkCore.Sqlite":
                    migrationBuilder.AddColumn<DateTime>(
                        name: "LiquidatedAt",
                        table: "Player_Shops",
                        type: "TEXT",
                        nullable: true
                    );

                    migrationBuilder.AddColumn<long>(
                        name: "LiquidatedGold",
                        table: "Player_Shops",
                        type: "INTEGER",
                        nullable: false,
                        defaultValue: 0L
                    );
                    break;

                case "Pomelo.EntityFrameworkCore.MySql":
                    migrationBuilder.AddColumn<DateTime>(
                        name: "LiquidatedAt",
                        table: "Player_Shops",
                        type: "datetime(6)",
                        nullable: true
                    );

                    migrationBuilder.AddColumn<long>(
                        name: "LiquidatedGold",
                        table: "Player_Shops",
                        type: "bigint",
                        nullable: false,
                        defaultValue: 0L
                    );
                    break;

                default:
                    throw new NotSupportedException(migrationBuilder.ActiveProvider);
            }
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LiquidatedAt",
                table: "Player_Shops"
            );

            migrationBuilder.DropColumn(
                name: "LiquidatedGold",
                table: "Player_Shops"
            );
        }
    }
}
