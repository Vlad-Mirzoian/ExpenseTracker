using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ExpenseTracker.Data.Migrations
{
    public partial class AddCategoryParentsTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create CategoryParents table
            migrationBuilder.CreateTable(
                name: "CategoryParents",
                columns: table => new
                {
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    ParentCategoryId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CategoryParents", x => new { x.CategoryId, x.ParentCategoryId });
                    table.ForeignKey(
                        name: "FK_CategoryParents_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CategoryParents_Categories_ParentCategoryId",
                        column: x => x.ParentCategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            // Migrate existing ParentCategoryId to CategoryParents
            migrationBuilder.Sql(@"
                INSERT INTO ""CategoryParents"" (""CategoryId"", ""ParentCategoryId"")
                SELECT ""Id"", ""ParentCategoryId""
                FROM ""Categories""
                WHERE ""ParentCategoryId"" IS NOT NULL;
            ");

            // Drop ParentCategoryId column and its constraints
            migrationBuilder.DropForeignKey(
                name: "FK_Categories_Categories_ParentCategoryId",
                table: "Categories");

            migrationBuilder.DropIndex(
                name: "IX_Categories_ParentCategoryId",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "ParentCategoryId",
                table: "Categories");

            migrationBuilder.CreateIndex(
                name: "IX_CategoryParents_ParentCategoryId",
                table: "CategoryParents",
                column: "ParentCategoryId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Re-add ParentCategoryId column
            migrationBuilder.AddColumn<Guid>(
                name: "ParentCategoryId",
                table: "Categories",
                type: "uuid",
                nullable: true);

            // Migrate back CategoryParents to ParentCategoryId (take first parent if multiple)
            migrationBuilder.Sql(@"
                UPDATE ""Categories""
                SET ""ParentCategoryId"" = (
                    SELECT ""ParentCategoryId""
                    FROM ""CategoryParents""
                    WHERE ""CategoryParents"".""CategoryId"" = ""Categories"".""Id""
                    LIMIT 1
                );
            ");

            // Drop CategoryParents table
            migrationBuilder.DropTable(
                name: "CategoryParents");

            // Recreate index and foreign key
            migrationBuilder.CreateIndex(
                name: "IX_Categories_ParentCategoryId",
                table: "Categories",
                column: "ParentCategoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_Categories_Categories_ParentCategoryId",
                table: "Categories",
                column: "ParentCategoryId",
                principalTable: "Categories",
                principalColumn: "Id");
        }
    }
}