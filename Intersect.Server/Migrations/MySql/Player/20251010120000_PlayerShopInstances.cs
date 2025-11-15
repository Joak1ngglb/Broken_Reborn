using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Intersect.Server.Migrations.MySql.Player
{
    /// <inheritdoc />
    public partial class PlayerShopInstances : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "MapInstanceId",
                table: "Player_Shops",
                type: "char(36)",
                nullable: false,
                defaultValue: Guid.Empty,
                collation: "ascii_general_ci");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MapInstanceId",
                table: "Player_Shops");
        }
    }
}
