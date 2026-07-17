using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Migrations
{
    /// <inheritdoc />
    public partial class UpdateOrderCompra : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DetalleCompra_Compra_Numero",
                table: "DetalleCompra");

            migrationBuilder.DropPrimaryKey(
                name: "PK_DetalleCompra",
                table: "DetalleCompra");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Compra",
                table: "Compra");

            migrationBuilder.AlterColumn<string>(
                name: "Numero",
                table: "DetalleCompra",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AddColumn<int>(
                name: "Id",
                table: "DetalleCompra",
                type: "int",
                nullable: false,
                defaultValue: 0)
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddColumn<int>(
                name: "OrdenCompraId",
                table: "DetalleCompra",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<string>(
                name: "Numero",
                table: "Compra",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AddColumn<int>(
                name: "Id",
                table: "Compra",
                type: "int",
                nullable: false,
                defaultValue: 0)
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddPrimaryKey(
                name: "PK_DetalleCompra",
                table: "DetalleCompra",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Compra",
                table: "Compra",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_DetalleCompra_OrdenCompraId",
                table: "DetalleCompra",
                column: "OrdenCompraId");

            migrationBuilder.AddForeignKey(
                name: "FK_DetalleCompra_Compra_OrdenCompraId",
                table: "DetalleCompra",
                column: "OrdenCompraId",
                principalTable: "Compra",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DetalleCompra_Compra_OrdenCompraId",
                table: "DetalleCompra");

            migrationBuilder.DropPrimaryKey(
                name: "PK_DetalleCompra",
                table: "DetalleCompra");

            migrationBuilder.DropIndex(
                name: "IX_DetalleCompra_OrdenCompraId",
                table: "DetalleCompra");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Compra",
                table: "Compra");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "DetalleCompra");

            migrationBuilder.DropColumn(
                name: "OrdenCompraId",
                table: "DetalleCompra");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "Compra");

            migrationBuilder.AlterColumn<string>(
                name: "Numero",
                table: "DetalleCompra",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Numero",
                table: "Compra",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddPrimaryKey(
                name: "PK_DetalleCompra",
                table: "DetalleCompra",
                column: "Numero");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Compra",
                table: "Compra",
                column: "Numero");

            migrationBuilder.AddForeignKey(
                name: "FK_DetalleCompra_Compra_Numero",
                table: "DetalleCompra",
                column: "Numero",
                principalTable: "Compra",
                principalColumn: "Numero",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
