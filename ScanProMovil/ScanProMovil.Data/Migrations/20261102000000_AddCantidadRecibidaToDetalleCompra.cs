using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ScanProMovil.Migrations
{
    /// <inheritdoc />
    public partial class AddCantidadRecibidaToDetalleCompra : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "CantidadRecibida",
                table: "PurchaseOrderItems",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CantidadRecibida",
                table: "PurchaseOrderItems");
        }
    }
}
