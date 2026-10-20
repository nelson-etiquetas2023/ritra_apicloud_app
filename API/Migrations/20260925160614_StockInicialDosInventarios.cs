using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Migrations
{
    /// <inheritdoc />
    public partial class StockInicialDosInventarios : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AjustesDetalles");

            migrationBuilder.DropTable(
                name: "AjustesIniciales");

            migrationBuilder.DropColumn(
                name: "CantidadFisica",
                table: "CargasInicialesDetalles");

            migrationBuilder.DropColumn(
                name: "FechaProcesado",
                table: "CargasInicialesDetalles");

            migrationBuilder.DropColumn(
                name: "Procesado",
                table: "CargasInicialesDetalles");

            migrationBuilder.DropColumn(
                name: "DocumentoFisico",
                table: "CargasIniciales");

            migrationBuilder.DropColumn(
                name: "DocumentoReferencia",
                table: "CargasIniciales");

            migrationBuilder.AlterColumn<double>(
                name: "CantidadFisica",
                table: "DocumentosFisicosDetalles",
                type: "float",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "Numero",
                table: "DocumentosFisicos",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaAplicacion",
                table: "DocumentosFisicos",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AlterColumn<double>(
                name: "Cantidad",
                table: "CargasInicialesDetalles",
                type: "float",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "Numero",
                table: "CargasIniciales",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentosFisicos_Numero",
                table: "DocumentosFisicos",
                column: "Numero",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CargasIniciales_Numero",
                table: "CargasIniciales",
                column: "Numero",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DocumentosFisicos_Numero",
                table: "DocumentosFisicos");

            migrationBuilder.DropIndex(
                name: "IX_CargasIniciales_Numero",
                table: "CargasIniciales");

            migrationBuilder.DropColumn(
                name: "FechaAplicacion",
                table: "DocumentosFisicos");

            migrationBuilder.AlterColumn<int>(
                name: "CantidadFisica",
                table: "DocumentosFisicosDetalles",
                type: "int",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "float");

            migrationBuilder.AlterColumn<string>(
                name: "Numero",
                table: "DocumentosFisicos",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<int>(
                name: "Cantidad",
                table: "CargasInicialesDetalles",
                type: "int",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "float");

            migrationBuilder.AddColumn<int>(
                name: "CantidadFisica",
                table: "CargasInicialesDetalles",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaProcesado",
                table: "CargasInicialesDetalles",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Procesado",
                table: "CargasInicialesDetalles",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "Numero",
                table: "CargasIniciales",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AddColumn<string>(
                name: "DocumentoFisico",
                table: "CargasIniciales",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DocumentoReferencia",
                table: "CargasIniciales",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "AjustesIniciales",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Comentario = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DocumentoFisico = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DocumentoReferencia = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Numero = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AjustesIniciales", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AjustesDetalles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AjusteId = table.Column<int>(type: "int", nullable: false),
                    CantidadAjuste = table.Column<int>(type: "int", nullable: false),
                    CantidadFisica = table.Column<int>(type: "int", nullable: false),
                    CantidadTeorica = table.Column<int>(type: "int", nullable: false),
                    Categoria = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Costo = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    FechaProcesado = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Nota = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Procesado = table.Column<bool>(type: "bit", nullable: false),
                    ProductCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProductName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Ubicacion = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Unidad = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AjustesDetalles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AjustesDetalles_AjustesIniciales_AjusteId",
                        column: x => x.AjusteId,
                        principalTable: "AjustesIniciales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AjustesDetalles_AjusteId",
                table: "AjustesDetalles",
                column: "AjusteId");
        }
    }
}
