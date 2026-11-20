using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BizSpace.Data.Migrations;

public partial class AddStoresAndNotifications : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Stores",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                ManagerId = table.Column<int>(type: "int", nullable: true),
                Name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Stores", x => x.Id);
                table.ForeignKey(
                    name: "FK_Stores_Users_ManagerId",
                    column: x => x.ManagerId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.SetNull);
            });

        migrationBuilder.CreateTable(
            name: "MarketingCampaigns",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                Name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                DiscountPercent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                StartsAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                EndsAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                StoreId = table.Column<int>(type: "int", nullable: true),
                CategoryId = table.Column<int>(type: "int", nullable: true),
                ProductId = table.Column<int>(type: "int", nullable: true),
                CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_MarketingCampaigns", x => x.Id);
                table.ForeignKey("FK_MarketingCampaigns_Categories_CategoryId", x => x.CategoryId,
                    "Categories", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_MarketingCampaigns_Products_ProductId", x => x.ProductId,
                    "Products", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_MarketingCampaigns_Stores_StoreId", x => x.StoreId,
                    "Stores", "Id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.Sql("""
            INSERT INTO [Stores] ([ManagerId], [Name], [Description], [CreatedAtUtc])
            VALUES (NULL, N'Legacy Catalog', N'Catalog data created before store ownership was introduced.', SYSUTCDATETIME());
            """);

        migrationBuilder.AddColumn<int>(
            name: "StoreId",
            table: "Categories",
            type: "int",
            nullable: true);

        migrationBuilder.Sql("""
            DECLARE @LegacyStoreId int;
            SELECT TOP (1) @LegacyStoreId = [Id]
            FROM [Stores]
            WHERE [ManagerId] IS NULL AND [Name] = N'Legacy Catalog'
            ORDER BY [Id];
            UPDATE [Categories] SET [StoreId] = @LegacyStoreId WHERE [StoreId] IS NULL;
            """);

        migrationBuilder.AlterColumn<int>(
            name: "StoreId",
            table: "Categories",
            type: "int",
            nullable: false,
            oldClrType: typeof(int),
            oldType: "int",
            oldNullable: true);

        migrationBuilder.AddColumn<string>(
            name: "Brand",
            table: "Products",
            type: "nvarchar(100)",
            maxLength: 100,
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<string>(
            name: "Specifications",
            table: "Products",
            type: "nvarchar(max)",
            nullable: false,
            defaultValue: "{}");

        migrationBuilder.CreateTable(
            name: "SavedAddresses",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                UserId = table.Column<int>(type: "int", nullable: false),
                Label = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                StreetAddress = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                City = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                PostalCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                Country = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SavedAddresses", x => x.Id);
                table.ForeignKey(
                    name: "FK_SavedAddresses_Users_UserId",
                    column: x => x.UserId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "Notifications",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                RecipientUserId = table.Column<int>(type: "int", nullable: false),
                SenderUserId = table.Column<int>(type: "int", nullable: true),
                StoreId = table.Column<int>(type: "int", nullable: true),
                RelatedEntityId = table.Column<int>(type: "int", nullable: true),
                Type = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                Title = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                Message = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                ReadAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Notifications", x => x.Id);
                table.ForeignKey(
                    name: "FK_Notifications_Stores_StoreId",
                    column: x => x.StoreId,
                    principalTable: "Stores",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.SetNull);
                table.ForeignKey(
                    name: "FK_Notifications_Users_RecipientUserId",
                    column: x => x.RecipientUserId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_Notifications_Users_SenderUserId",
                    column: x => x.SenderUserId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.NoAction);
            });

        migrationBuilder.CreateIndex(
            name: "IX_MarketingCampaigns_CategoryId",
            table: "MarketingCampaigns",
            column: "CategoryId");

        migrationBuilder.CreateIndex(
            name: "IX_MarketingCampaigns_ProductId",
            table: "MarketingCampaigns",
            column: "ProductId");

        migrationBuilder.CreateIndex(
            name: "IX_MarketingCampaigns_StoreId",
            table: "MarketingCampaigns",
            column: "StoreId");

        migrationBuilder.CreateIndex(
            name: "IX_MarketingCampaigns_StartsAtUtc_EndsAtUtc_IsEnabled",
            table: "MarketingCampaigns",
            columns: new[] { "StartsAtUtc", "EndsAtUtc", "IsEnabled" });

        migrationBuilder.CreateIndex(
            name: "IX_Categories_StoreId",
            table: "Categories",
            column: "StoreId");

        migrationBuilder.CreateIndex(
            name: "IX_Notifications_RecipientUserId_CreatedAtUtc",
            table: "Notifications",
            columns: new[] { "RecipientUserId", "CreatedAtUtc" });

        migrationBuilder.CreateIndex(
            name: "IX_Notifications_SenderUserId",
            table: "Notifications",
            column: "SenderUserId");

        migrationBuilder.CreateIndex(
            name: "IX_Notifications_StoreId",
            table: "Notifications",
            column: "StoreId");

        migrationBuilder.CreateIndex(
            name: "IX_Stores_ManagerId_Name",
            table: "Stores",
            columns: new[] { "ManagerId", "Name" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_SavedAddresses_UserId",
            table: "SavedAddresses",
            column: "UserId");

        migrationBuilder.AddForeignKey(
            name: "FK_Categories_Stores_StoreId",
            table: "Categories",
            column: "StoreId",
            principalTable: "Stores",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_Categories_Stores_StoreId",
            table: "Categories");

        migrationBuilder.DropTable(name: "Notifications");
        migrationBuilder.DropTable(name: "SavedAddresses");
        migrationBuilder.DropTable(name: "MarketingCampaigns");

        migrationBuilder.DropIndex(
            name: "IX_Categories_StoreId",
            table: "Categories");

        migrationBuilder.DropColumn(
            name: "StoreId",
            table: "Categories");

        migrationBuilder.DropColumn(name: "Brand", table: "Products");
        migrationBuilder.DropColumn(name: "Specifications", table: "Products");

        migrationBuilder.DropTable(name: "Stores");
    }
}
