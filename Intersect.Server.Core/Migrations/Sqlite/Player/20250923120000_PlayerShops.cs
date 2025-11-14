using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Intersect.Server.Migrations.Sqlite.Player
{
    /// <inheritdoc />
    public partial class PlayerShops : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Player_Shops",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    OwnerId = table.Column<Guid>(type: "TEXT", nullable: false),
                    MapId = table.Column<Guid>(type: "TEXT", nullable: false),
                    X = table.Column<int>(type: "INTEGER", nullable: false),
                    Y = table.Column<int>(type: "INTEGER", nullable: false),
                    Z = table.Column<int>(type: "INTEGER", nullable: false),
                    Title = table.Column<string>(type: "TEXT", nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    PendingGold = table.Column<long>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ClosedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Player_Shops", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Player_Shops_Players_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Player_ShopItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ShopId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ItemId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Quantity = table.Column<int>(type: "INTEGER", nullable: false),
                    PricePerUnit = table.Column<int>(type: "INTEGER", nullable: false),
                    IsSold = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    SoldAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ItemProperties = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Player_ShopItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Player_ShopItems_Player_Shops_ShopId",
                        column: x => x.ShopId,
                        principalTable: "Player_Shops",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Player_ShopTransactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ShopId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ShopItemId = table.Column<Guid>(type: "TEXT", nullable: false),
                    OwnerId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ItemId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Quantity = table.Column<int>(type: "INTEGER", nullable: false),
                    UnitPrice = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalPrice = table.Column<int>(type: "INTEGER", nullable: false),
                    BuyerName = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ItemProperties = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Player_ShopTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Player_ShopTransactions_Player_Shops_ShopId",
                        column: x => x.ShopId,
                        principalTable: "Player_Shops",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Player_ShopTransactions_Player_ShopItems_ShopItemId",
                        column: x => x.ShopItemId,
                        principalTable: "Player_ShopItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Player_ShopTransactions_Players_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Player_ShopItems_ShopId",
                table: "Player_ShopItems",
                column: "ShopId");

            migrationBuilder.CreateIndex(
                name: "IX_Player_Shops_ExpiresAt",
                table: "Player_Shops",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_Player_Shops_MapId_Status",
                table: "Player_Shops",
                columns: new[] { "MapId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Player_Shops_OwnerId",
                table: "Player_Shops",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_Player_Shops_OwnerId_Status",
                table: "Player_Shops",
                columns: new[] { "OwnerId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Player_ShopTransactions_OwnerId",
                table: "Player_ShopTransactions",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_Player_ShopTransactions_ShopId",
                table: "Player_ShopTransactions",
                column: "ShopId");

            migrationBuilder.CreateIndex(
                name: "IX_Player_ShopTransactions_ShopItemId",
                table: "Player_ShopTransactions",
                column: "ShopItemId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Player_ShopTransactions");

            migrationBuilder.DropTable(
                name: "Player_ShopItems");

            migrationBuilder.DropTable(
                name: "Player_Shops");
        }
    }
}
