using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BizSpace.Data.Migrations;

public partial class ProductImageManagement : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "ProductImageDeletionJobs",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                PublicId = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                LastAttemptAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                Attempts = table.Column<int>(type: "int", nullable: false),
                LastError = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
            },
            constraints: table => table.PrimaryKey("PK_ProductImageDeletionJobs", x => x.Id));

        migrationBuilder.CreateTable(
            name: "ProductImages",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                ProductId = table.Column<int>(type: "int", nullable: false),
                PublicId = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                SecureUrl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                Format = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                Width = table.Column<int>(type: "int", nullable: true),
                Height = table.Column<int>(type: "int", nullable: true),
                SortOrder = table.Column<int>(type: "int", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ProductImages", x => x.Id);
                table.ForeignKey(
                    name: "FK_ProductImages_Products_ProductId",
                    column: x => x.ProductId,
                    principalTable: "Products",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_ProductImageDeletionJobs_CreatedAtUtc",
            table: "ProductImageDeletionJobs",
            column: "CreatedAtUtc");

        migrationBuilder.CreateIndex(
            name: "IX_ProductImages_ProductId_SortOrder",
            table: "ProductImages",
            columns: new[] { "ProductId", "SortOrder" });

        migrationBuilder.Sql("""
            INSERT INTO ProductImages (ProductId, PublicId, SecureUrl, SortOrder, CreatedAtUtc)
            SELECT p.Id, NULL, image.[value], TRY_CONVERT(int, image.[key]), SYSUTCDATETIME()
            FROM Products AS p
            CROSS APPLY OPENJSON(CASE WHEN ISJSON(p.Images) = 1 THEN p.Images ELSE N'[]' END) AS image
            WHERE image.[type] = 1 AND LEN(image.[value]) > 0;
            """);

        migrationBuilder.DropColumn(name: "Images", table: "Products");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "Images",
            table: "Products",
            type: "nvarchar(max)",
            nullable: false,
            defaultValue: "[]");

        migrationBuilder.Sql("""
            UPDATE product
            SET Images = COALESCE((
                SELECT CONCAT('[', STRING_AGG(CONCAT('"', STRING_ESCAPE(image.SecureUrl, 'json'), '"'), ',')
                    WITHIN GROUP (ORDER BY image.SortOrder), ']')
                FROM ProductImages AS image
                WHERE image.ProductId = product.Id), '[]')
            FROM Products AS product;
            """);

        migrationBuilder.DropTable(name: "ProductImageDeletionJobs");
        migrationBuilder.DropTable(name: "ProductImages");
    }
}
