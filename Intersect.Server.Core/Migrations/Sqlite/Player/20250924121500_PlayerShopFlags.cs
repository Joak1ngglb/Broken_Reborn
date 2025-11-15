using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Intersect.Server.Migrations.Sqlite.Player
{
    /// <inheritdoc />
    public partial class PlayerShopFlags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ActivePlayerShopId",
                table: "Players",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ActivePlayerShopStatus",
                table: "Players",
                type: "INTEGER",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ActivePlayerShopId",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "ActivePlayerShopStatus",
                table: "Players");
        }
    }
}
