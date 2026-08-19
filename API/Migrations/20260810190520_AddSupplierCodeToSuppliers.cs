using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Migrations
{
    /// <inheritdoc />
    public partial class AddSupplierCodeToSuppliers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SupplierCode",
                table: "Suppliers",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql(@"
WITH cte AS
(
    SELECT SupplierId,
           ROW_NUMBER() OVER (ORDER BY SupplierId) AS rn
    FROM Suppliers
)
UPDATE s
SET s.SupplierCode = 'P' + RIGHT('000000' + CAST(c.rn AS VARCHAR(6)), 6)
FROM Suppliers s
INNER JOIN cte c ON c.SupplierId = s.SupplierId;
");

            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_SupplierCode",
                table: "Suppliers",
                column: "SupplierCode",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Suppliers_SupplierCode",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "SupplierCode",
                table: "Suppliers");
        }
    }
}
