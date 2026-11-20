using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BizSpace.Data.Migrations;

public partial class CategoryImageCloudinary : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "ImagePublicId",
            table: "Categories",
            type: "nvarchar(512)",
            maxLength: 512,
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "ImagePublicId",
            table: "Categories");
    }
}
