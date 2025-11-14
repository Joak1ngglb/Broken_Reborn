using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Intersect.Server.Migrations.MySql.Player
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
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    OwnerId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    MapId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    X = table.Column<int>(type: "int", nullable: false),
                    Y = table.Column<int>(type: "int", nullable: false),
                    Z = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "longtext", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    PendingGold = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ClosedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "longblob", rowVersion: true, nullable: true)
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
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Player_ShopItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    ShopId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    ItemId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    PricePerUnit = table.Column<int>(type: "int", nullable: false),
                    IsSold = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    SoldAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ItemProperties = table.Column<string>(type: "longtext", nullable: true)
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
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Player_ShopTransactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    ShopId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    ShopItemId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    OwnerId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    ItemId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    UnitPrice = table.Column<int>(type: "int", nullable: false),
                    TotalPrice = table.Column<int>(type: "int", nullable: false),
                    BuyerName = table.Column<string>(type: "longtext", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ItemProperties = table.Column<string>(type: "longtext", nullable: true)
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
                })
                .Annotation("MySql:CharSet", "utf8mb4");

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
