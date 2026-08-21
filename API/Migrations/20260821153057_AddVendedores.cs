using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Migrations
{
    /// <inheritdoc />
    public partial class AddVendedores : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Vendedor",
                table: "PedidoVenta",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Contacto",
                table: "Customers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "Vendedores",
                columns: table => new
                {
                    vendedor_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    vendedor_code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    vendedor_name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    telefono = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vendedores", x => x.vendedor_id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Vendedores_vendedor_code",
                table: "Vendedores",
                column: "vendedor_code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Vendedores");

            migrationBuilder.DropColumn(
                name: "Vendedor",
                table: "PedidoVenta");

            migrationBuilder.DropColumn(
                name: "Contacto",
                table: "Customers");
        }
    }
}
