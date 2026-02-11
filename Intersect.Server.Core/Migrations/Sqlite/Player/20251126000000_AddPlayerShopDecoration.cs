using Intersect.Framework.Core.Entities;
using Intersect.Server.Database.PlayerData;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Intersect.Server.Migrations.Sqlite.Player
{
    [DbContext(typeof(SqlitePlayerContext))]
    [Migration("20251126000000_AddPlayerShopDecoration")]
    /// <inheritdoc />
    public partial class AddPlayerShopDecoration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Decoration",
                table: "Player_Shops",
                type: "TEXT",
                nullable: false,
                defaultValue: PlayerShopEntityConstants.DefaultDecoration
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Decoration",
                table: "Player_Shops"
            );
        }
    }
}
