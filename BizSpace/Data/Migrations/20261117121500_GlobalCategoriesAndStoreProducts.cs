using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BizSpace.Data.Migrations;

public partial class GlobalCategoriesAndStoreProducts : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "StoreId",
            table: "Products",
            type: "int",
            nullable: true);

        migrationBuilder.Sql("""
            UPDATE p
            SET StoreId = c.StoreId
            FROM Products AS p
            INNER JOIN Categories AS c ON c.Id = p.CategoryId;
            """);

        migrationBuilder.AlterColumn<int>(
            name: "StoreId",
            table: "Products",
            type: "int",
            nullable: false,
            oldClrType: typeof(int),
            oldType: "int",
            oldNullable: true);

        migrationBuilder.Sql("""
            UPDATE m
            SET StoreId = c.StoreId
            FROM MarketingCampaigns AS m
            INNER JOIN Categories AS c ON c.Id = m.CategoryId
            WHERE m.CategoryId IS NOT NULL
              AND m.StoreId IS NULL
              AND m.ProductId IS NULL;
            """);

        migrationBuilder.DropForeignKey(
            name: "FK_Categories_Stores_StoreId",
            table: "Categories");

        migrationBuilder.DropIndex(
            name: "IX_Categories_StoreId",
            table: "Categories");

        migrationBuilder.CreateIndex(
            name: "IX_Products_StoreId",
            table: "Products",
            column: "StoreId");

        migrationBuilder.AddForeignKey(
            name: "FK_Products_Stores_StoreId",
            table: "Products",
            column: "StoreId",
            principalTable: "Stores",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.DropColumn(
            name: "StoreId",
            table: "Categories");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "StoreId",
            table: "Categories",
            type: "int",
            nullable: true);

        migrationBuilder.Sql("""
            DECLARE @LegacyStoreId int;
            SELECT TOP (1) @LegacyStoreId = Id
            FROM Stores
            WHERE ManagerId IS NULL AND Name = N'Legacy Catalog'
            ORDER BY Id;

            UPDATE c
            SET StoreId = COALESCE(
                (SELECT MIN(p.StoreId) FROM Products AS p WHERE p.CategoryId = c.Id),
                @LegacyStoreId)
            FROM Categories AS c;

            UPDATE MarketingCampaigns
            SET StoreId = NULL
            WHERE CategoryId IS NOT NULL AND ProductId IS NULL;
            """);

        migrationBuilder.AlterColumn<int>(
            name: "StoreId",
            table: "Categories",
            type: "int",
            nullable: false,
            oldClrType: typeof(int),
            oldType: "int",
            oldNullable: true);

        migrationBuilder.DropForeignKey(
            name: "FK_Products_Stores_StoreId",
            table: "Products");

        migrationBuilder.DropIndex(
            name: "IX_Products_StoreId",
            table: "Products");

        migrationBuilder.CreateIndex(
            name: "IX_Categories_StoreId",
            table: "Categories",
            column: "StoreId");

        migrationBuilder.AddForeignKey(
            name: "FK_Categories_Stores_StoreId",
            table: "Categories",
            column: "StoreId",
            principalTable: "Stores",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.DropColumn(
            name: "StoreId",
            table: "Products");
    }
}
