using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ScanProMovil.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueCodeBarIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Products_CodeBar",
                table: "Products");

            migrationBuilder.CreateIndex(
                name: "IX_Products_CodeBar",
                table: "Products",
                column: "CodeBar",
                unique: true,
                filter: "CodeBar IS NOT NULL AND CodeBar <> ''");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Products_CodeBar",
                table: "Products");

            migrationBuilder.CreateIndex(
                name: "IX_Products_CodeBar",
                table: "Products",
                column: "CodeBar");
        }
    }
}
