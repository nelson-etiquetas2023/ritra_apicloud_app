using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Migrations
{
    /// <inheritdoc />
    public partial class RenameRegistroFiscalToRNC : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Cliente_Ruc",
                table: "PedidoVenta",
                newName: "Cliente_RNC");

            migrationBuilder.RenameColumn(
                name: "Registro_Fiscal",
                table: "Customers",
                newName: "RNC");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Cliente_RNC",
                table: "PedidoVenta",
                newName: "Cliente_Ruc");

            migrationBuilder.RenameColumn(
                name: "RNC",
                table: "Customers",
                newName: "Registro_Fiscal");
        }
    }
}
