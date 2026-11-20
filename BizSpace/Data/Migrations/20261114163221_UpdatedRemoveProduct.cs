using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BizSpace.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdatedRemoveProduct : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // IX_Products_CategoryId was already removed by the preceding
            // RemoveProductStatusAndEnableHardDelete migration.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // The preceding migration's model does not include this index.
        }
    }
}
