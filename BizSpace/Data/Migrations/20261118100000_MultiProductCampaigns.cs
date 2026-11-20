using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BizSpace.Data.Migrations;

public partial class MultiProductCampaigns : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "MarketingCampaignProducts",
            columns: table => new
            {
                CampaignId = table.Column<int>(type: "int", nullable: false),
                ProductId = table.Column<int>(type: "int", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_MarketingCampaignProducts", x => new { x.CampaignId, x.ProductId });
                table.ForeignKey(
                    name: "FK_MarketingCampaignProducts_MarketingCampaigns_CampaignId",
                    column: x => x.CampaignId,
                    principalTable: "MarketingCampaigns",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_MarketingCampaignProducts_Products_ProductId",
                    column: x => x.ProductId,
                    principalTable: "Products",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_MarketingCampaignProducts_ProductId",
            table: "MarketingCampaignProducts",
            column: "ProductId");

        migrationBuilder.Sql("""
            INSERT INTO MarketingCampaignProducts (CampaignId, ProductId)
            SELECT Id, ProductId
            FROM MarketingCampaigns
            WHERE ProductId IS NOT NULL;
            """);

        migrationBuilder.DropForeignKey(
            name: "FK_MarketingCampaigns_Products_ProductId",
            table: "MarketingCampaigns");

        migrationBuilder.DropIndex(
            name: "IX_MarketingCampaigns_ProductId",
            table: "MarketingCampaigns");

        migrationBuilder.DropColumn(
            name: "ProductId",
            table: "MarketingCampaigns");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "ProductId",
            table: "MarketingCampaigns",
            type: "int",
            nullable: true);

        migrationBuilder.Sql("""
            UPDATE campaign
            SET ProductId = (
                SELECT MIN(link.ProductId)
                FROM MarketingCampaignProducts AS link
                WHERE link.CampaignId = campaign.Id)
            FROM MarketingCampaigns AS campaign
            WHERE EXISTS (
                SELECT 1
                FROM MarketingCampaignProducts AS link
                WHERE link.CampaignId = campaign.Id);
            """);

        migrationBuilder.CreateIndex(
            name: "IX_MarketingCampaigns_ProductId",
            table: "MarketingCampaigns",
            column: "ProductId");

        migrationBuilder.AddForeignKey(
            name: "FK_MarketingCampaigns_Products_ProductId",
            table: "MarketingCampaigns",
            column: "ProductId",
            principalTable: "Products",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.DropTable(
            name: "MarketingCampaignProducts");
    }
}
