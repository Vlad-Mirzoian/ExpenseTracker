using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ExpenseTracker.Data.Migrations
{
    /// <inheritdoc />
    public partial class updateUserAndDefaultMccCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    MccCodes = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Login = table.Column<string>(type: "text", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: false),
                    Token = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Transactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    Date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    MccCode = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Transactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Transactions_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Transactions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Categories",
                columns: new[] { "Id", "MccCodes", "Name" },
                values: new object[,]
                {
                    { new Guid("1a12c08c-f9eb-4f29-8480-ef05137e0cf5"), "5912", "Аптеки" },
                    { new Guid("2afd336a-03b8-4f17-8cfe-2c0e93cb4c49"), "4829", "Знаття/Відправка" },
                    { new Guid("61e1f6c7-7b85-47d1-bb9a-d78f911e8cd3"), "5541", "АЗС" },
                    { new Guid("66666666-6666-6666-6666-666666666666"), "0", "Інше" },
                    { new Guid("74d258ea-bf82-4934-bb9a-8a343d9da1ea"), "7832", "Кінотеатри" },
                    { new Guid("86cdeb09-ae28-4a8f-9006-24ce4c44419f"), "6012", "Надходження" },
                    { new Guid("a25a42f4-88b3-4006-b0c3-2c7a15a358e7"), "5411,5499", "Супермаркети" },
                    { new Guid("ad42b743-ef9a-43e5-b71f-97a742ae1a85"), "5651", "Магазини одягу" },
                    { new Guid("b50ee9f0-3480-40d8-bb48-e15bf9e2fc03"), "5811,8999", "Доставка" },
                    { new Guid("c1b15d7e-0b6f-4d19-9d8c-b0c8722277d0"), "5814,5812,5462", "Кафе/ресторани" },
                    { new Guid("cf668835-71a2-4868-87b8-3938ac9dabf9"), "4900", "Комуналка" },
                    { new Guid("d177f3d7-d5d2-4d97-bd9e-45f54e2e268f"), "7011", "Готелі" },
                    { new Guid("db60d0b9-89e6-4694-9295-56b688254a2f"), "6011", "Банки" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_CategoryId",
                table: "Transactions",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_UserId",
                table: "Transactions",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Transactions");

            migrationBuilder.DropTable(
                name: "Categories");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
