using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BizSpace.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddedNewRole : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Stores_ManagerId_Name",
                table: "Stores");

            migrationBuilder.CreateIndex(
                name: "IX_Stores_ManagerId_Name",
                table: "Stores",
                columns: new[] { "ManagerId", "Name" },
                unique: true,
                filter: "[ManagerId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Stores_ManagerId_Name",
                table: "Stores");

            migrationBuilder.CreateIndex(
                name: "IX_Stores_ManagerId_Name",
                table: "Stores",
                columns: new[] { "ManagerId", "Name" },
                unique: true);
        }
    }
}
