using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Migrations
{
    /// <inheritdoc />
    public partial class uiintegracionwebmovil : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "Impuesto",
                table: "Compra",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<string>(
                name: "Reference",
                table: "Compra",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<double>(
                name: "Subtotal",
                table: "Compra",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<int>(
                name: "Supply_Id",
                table: "Compra",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Supply_Name",
                table: "Compra",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Impuesto",
                table: "Compra");

            migrationBuilder.DropColumn(
                name: "Reference",
                table: "Compra");

            migrationBuilder.DropColumn(
                name: "Subtotal",
                table: "Compra");

            migrationBuilder.DropColumn(
                name: "Supply_Id",
                table: "Compra");

            migrationBuilder.DropColumn(
                name: "Supply_Name",
                table: "Compra");
        }
    }
}
