using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerCodeToCustomers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CustomerCode",
                table: "Customers",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CustomerName",
                table: "Customers",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql(@"
WITH cte AS
(
    SELECT customer_id,
           ROW_NUMBER() OVER (ORDER BY customer_id) AS rn
    FROM Customers
)
UPDATE c
SET c.CustomerCode = 'C' + RIGHT('000000' + CAST(cte.rn AS VARCHAR(6)), 6),
    c.CustomerName = CASE WHEN LTRIM(RTRIM(c.CustomerName)) = '' THEN LTRIM(RTRIM(c.Direccion)) ELSE LTRIM(RTRIM(c.CustomerName)) END
FROM Customers c
INNER JOIN cte ON cte.customer_id = c.customer_id;
");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_CustomerCode",
                table: "Customers",
                column: "CustomerCode",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Customers_CustomerCode",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "CustomerCode",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "CustomerName",
                table: "Customers");
        }
    }
}
