using Intersect.Server.Database.PlayerData;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Intersect.Server.Migrations.Sqlite.Player
{
    [DbContext(typeof(SqlitePlayerContext))]
    [Migration("20260210113000_ActivePlayerShopUniqueness")]
    public partial class ActivePlayerShopUniqueness : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            switch (migrationBuilder.ActiveProvider)
            {
                case "Microsoft.EntityFrameworkCore.Sqlite":
                    migrationBuilder.AddColumn<int>(
                        name: "ActiveUniquenessToken",
                        table: "Player_Shops",
                        type: "INTEGER",
                        nullable: true
                    );

                    migrationBuilder.Sql(
                        "UPDATE Player_Shops SET ActiveUniquenessToken = 1 WHERE Status = 0;"
                    );

                    migrationBuilder.CreateIndex(
                        name: "IX_Player_Shops_OwnerId_ActiveUniquenessToken",
                        table: "Player_Shops",
                        columns: new[] { "OwnerId", "ActiveUniquenessToken" },
                        unique: true
                    );

                    migrationBuilder.CreateIndex(
                        name: "IX_Player_Shops_MapId_MapInstanceId_X_Y_Z_ActiveUniquenessToken",
                        table: "Player_Shops",
                        columns: new[] { "MapId", "MapInstanceId", "X", "Y", "Z", "ActiveUniquenessToken" },
                        unique: true
                    );
                    break;

                case "Pomelo.EntityFrameworkCore.MySql":
                    migrationBuilder.AddColumn<int>(
                        name: "ActiveUniquenessToken",
                        table: "Player_Shops",
                        type: "int",
                        nullable: true
                    );

                    migrationBuilder.Sql(
                        "UPDATE Player_Shops SET ActiveUniquenessToken = 1 WHERE Status = 0;"
                    );

                    migrationBuilder.CreateIndex(
                        name: "IX_Player_Shops_OwnerId_ActiveUniquenessToken",
                        table: "Player_Shops",
                        columns: new[] { "OwnerId", "ActiveUniquenessToken" },
                        unique: true
                    );

                    migrationBuilder.CreateIndex(
                        name: "IX_Player_Shops_MapId_MapInstanceId_X_Y_Z_ActiveUniquenessToken",
                        table: "Player_Shops",
                        columns: new[] { "MapId", "MapInstanceId", "X", "Y", "Z", "ActiveUniquenessToken" },
                        unique: true
                    );
                    break;

                default:
                    throw new NotSupportedException(migrationBuilder.ActiveProvider);
            }
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Player_Shops_OwnerId_ActiveUniquenessToken",
                table: "Player_Shops"
            );

            migrationBuilder.DropIndex(
                name: "IX_Player_Shops_MapId_MapInstanceId_X_Y_Z_ActiveUniquenessToken",
                table: "Player_Shops"
            );

            migrationBuilder.DropColumn(
                name: "ActiveUniquenessToken",
                table: "Player_Shops"
            );
        }
    }
}
