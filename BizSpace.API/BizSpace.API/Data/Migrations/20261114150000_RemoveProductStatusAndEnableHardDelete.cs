using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BizSpace.Data.Migrations;

public partial class RemoveProductStatusAndEnableHardDelete : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_Products_Status_CategoryId_Price",
            table: "Products");

        migrationBuilder.Sql("""
            IF EXISTS (
                SELECT 1
                FROM sys.indexes
                WHERE [name] = N'IX_Products_CategoryId'
                  AND [object_id] = OBJECT_ID(N'[dbo].[Products]'))
            BEGIN
                DROP INDEX [IX_Products_CategoryId] ON [dbo].[Products];
            END;
            """);

        migrationBuilder.DropForeignKey(
            name: "FK_OrderItems_Products_ProductId",
            table: "OrderItems");

        migrationBuilder.AlterColumn<int>(
            name: "ProductId",
            table: "OrderItems",
            type: "int",
            nullable: true,
            oldClrType: typeof(int),
            oldType: "int");

        migrationBuilder.AddForeignKey(
            name: "FK_OrderItems_Products_ProductId",
            table: "OrderItems",
            column: "ProductId",
            principalTable: "Products",
            principalColumn: "Id",
            onDelete: ReferentialAction.SetNull);

        migrationBuilder.DropColumn(
            name: "Status",
            table: "Products");

        migrationBuilder.CreateIndex(
            name: "IX_Products_CategoryId_Price",
            table: "Products",
            columns: new[] { "CategoryId", "Price" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            IF EXISTS (SELECT 1 FROM [OrderItems] WHERE [ProductId] IS NULL)
            BEGIN
                THROW 51000, 'Cannot revert this migration because deleted products are referenced by order snapshots.', 1;
            END;
            """);

        migrationBuilder.DropIndex(
            name: "IX_Products_CategoryId_Price",
            table: "Products");

        migrationBuilder.DropForeignKey(
            name: "FK_OrderItems_Products_ProductId",
            table: "OrderItems");

        migrationBuilder.AlterColumn<int>(
            name: "ProductId",
            table: "OrderItems",
            type: "int",
            nullable: false,
            oldClrType: typeof(int),
            oldType: "int",
            oldNullable: true);

        migrationBuilder.AddForeignKey(
            name: "FK_OrderItems_Products_ProductId",
            table: "OrderItems",
            column: "ProductId",
            principalTable: "Products",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddColumn<string>(
            name: "Status",
            table: "Products",
            type: "nvarchar(20)",
            maxLength: 20,
            nullable: false,
            defaultValue: "Active");

        migrationBuilder.CreateIndex(
            name: "IX_Products_Status_CategoryId_Price",
            table: "Products",
            columns: new[] { "Status", "CategoryId", "Price" });

        migrationBuilder.CreateIndex(
            name: "IX_Products_CategoryId",
            table: "Products",
            column: "CategoryId");
    }
}
