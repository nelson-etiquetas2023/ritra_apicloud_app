using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Migrations
{
    /// <inheritdoc />
    public partial class RemoveCamposPedidoVenta : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Comentario",
                table: "PedidoVenta");

            migrationBuilder.DropColumn(
                name: "CondicionPago",
                table: "PedidoVenta");

            migrationBuilder.DropColumn(
                name: "DiasCredito",
                table: "PedidoVenta");

            migrationBuilder.DropColumn(
                name: "Vendedor",
                table: "PedidoVenta");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Comentario",
                table: "PedidoVenta",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CondicionPago",
                table: "PedidoVenta",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "DiasCredito",
                table: "PedidoVenta",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Vendedor",
                table: "PedidoVenta",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
