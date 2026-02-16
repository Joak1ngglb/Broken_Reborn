using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Intersect.Server.Migrations.Sqlite.Game
{
    /// <inheritdoc />
    public partial class AddNpcBossFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "BossAnnounceOnKill",
                table: "Npcs",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "BossAnnounceOnRespawn",
                table: "Npcs",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "BossRespawnMinutes",
                table: "Npcs",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsBoss",
                table: "Npcs",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BossAnnounceOnKill",
                table: "Npcs");

            migrationBuilder.DropColumn(
                name: "BossAnnounceOnRespawn",
                table: "Npcs");

            migrationBuilder.DropColumn(
                name: "BossRespawnMinutes",
                table: "Npcs");

            migrationBuilder.DropColumn(
                name: "IsBoss",
                table: "Npcs");
        }
    }
}
