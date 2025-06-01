using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExpenseTracker.Data.Migrations
{
    /// <inheritdoc />
    public partial class CategoryEnhancements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CategoryParents");

            migrationBuilder.CreateTable(
                name: "CategoryRelationships",
                columns: table => new
                {
                    CustomCategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    BaseCategoryId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CategoryRelationships", x => new { x.CustomCategoryId, x.BaseCategoryId });
                    table.ForeignKey(
                        name: "FK_CategoryRelationships_Categories_BaseCategoryId",
                        column: x => x.BaseCategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CategoryRelationships_Categories_CustomCategoryId",
                        column: x => x.CustomCategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TransactionCategories",
                columns: table => new
                {
                    TransactionId = table.Column<Guid>(type: "uuid", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsBaseCategory = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransactionCategories", x => new { x.TransactionId, x.CategoryId });
                    table.ForeignKey(
                        name: "FK_TransactionCategories_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TransactionCategories_Transactions_TransactionId",
                        column: x => x.TransactionId,
                        principalTable: "Transactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql(@"
                INSERT INTO ""TransactionCategories"" (""TransactionId"", ""CategoryId"", ""IsBaseCategory"")
                SELECT ""Id"", ""CategoryId"", true
                FROM ""Transactions""
                WHERE ""CategoryId"" IS NOT NULL
                AND EXISTS (
                    SELECT 1 FROM ""Categories"" WHERE ""Id"" = ""Transactions"".""CategoryId""
                );
            ");

            migrationBuilder.CreateIndex(
                name: "IX_CategoryRelationships_BaseCategoryId",
                table: "CategoryRelationships",
                column: "BaseCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_TransactionCategories_CategoryId",
                table: "TransactionCategories",
                column: "CategoryId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CategoryRelationships");

            migrationBuilder.DropTable(
                name: "TransactionCategories");

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

            migrationBuilder.CreateIndex(
                name: "IX_CategoryParents_ParentCategoryId",
                table: "CategoryParents",
                column: "ParentCategoryId");
        }
    }
}
