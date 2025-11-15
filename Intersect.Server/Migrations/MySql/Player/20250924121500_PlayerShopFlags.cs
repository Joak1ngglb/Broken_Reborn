using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Intersect.Server.Migrations.MySql.Player
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
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.AddColumn<int>(
                name: "ActivePlayerShopStatus",
                table: "Players",
                type: "int",
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
