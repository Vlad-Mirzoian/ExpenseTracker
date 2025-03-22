using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ExpenseTracker.Data.Migrations
{
    /// <inheritdoc />
    public partial class updateMccCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("a25a42f4-88b3-4006-b0c3-2c7a15a358e7"),
                column: "MccCodes",
                value: "5411,5499");

            migrationBuilder.InsertData(
                table: "Categories",
                columns: new[] { "Id", "MccCodes", "Name" },
                values: new object[,]
                {
                    { new Guid("2afd336a-03b8-4f17-8cfe-2c0e93cb4c49"), "4829", "Знаття/Відправка" },
                    { new Guid("86cdeb09-ae28-4a8f-9006-24ce4c44419f"), "6012", "Надходження" },
                    { new Guid("b50ee9f0-3480-40d8-bb48-e15bf9e2fc03"), "5811,8999", "Доставка" },
                    { new Guid("cf668835-71a2-4868-87b8-3938ac9dabf9"), "4900", "Комуналка" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("2afd336a-03b8-4f17-8cfe-2c0e93cb4c49"));

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("86cdeb09-ae28-4a8f-9006-24ce4c44419f"));

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("b50ee9f0-3480-40d8-bb48-e15bf9e2fc03"));

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("cf668835-71a2-4868-87b8-3938ac9dabf9"));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("a25a42f4-88b3-4006-b0c3-2c7a15a358e7"),
                column: "MccCodes",
                value: "5411");
        }
    }
}
