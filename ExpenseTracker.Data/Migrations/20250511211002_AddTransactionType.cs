using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace ExpenseTracker.Data.Migrations
{
    public partial class AddTransactionType : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TransactionType",
                table: "Transactions",
                type: "text",
                nullable: false,
                defaultValue: "Expense");

            migrationBuilder.Sql("UPDATE \"Transactions\" SET \"TransactionType\" = CASE WHEN \"Amount\" < 0 THEN 'Income' ELSE 'Expense' END WHERE \"TransactionType\" IS NULL");

        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TransactionType",
                table: "Transactions");
        }
    }
}