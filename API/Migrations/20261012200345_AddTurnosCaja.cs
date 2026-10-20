using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Migrations
{
    /// <inheritdoc />
    public partial class AddTurnosCaja : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TurnoId",
                table: "PedidoVenta",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TurnoDenominaciones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TurnoId = table.Column<int>(type: "int", nullable: false),
                    Denominacion = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Cantidad = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TurnoDenominaciones", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TurnoMovimientos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TurnoId = table.Column<int>(type: "int", nullable: false),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Monto = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Motivo = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TurnoMovimientos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TurnosCaja",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Caja = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Cajero = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FechaApertura = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FondoInicial = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    FechaCierre = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Observacion = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Esperado = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Contado = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Diferencia = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TurnosCaja", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PedidoVenta_TurnoId",
                table: "PedidoVenta",
                column: "TurnoId");

            migrationBuilder.CreateIndex(
                name: "IX_TurnoDenominaciones_TurnoId",
                table: "TurnoDenominaciones",
                column: "TurnoId");

            migrationBuilder.CreateIndex(
                name: "IX_TurnoMovimientos_TurnoId",
                table: "TurnoMovimientos",
                column: "TurnoId");

            migrationBuilder.CreateIndex(
                name: "IX_TurnosCaja_Caja_Status",
                table: "TurnosCaja",
                columns: new[] { "Caja", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TurnoDenominaciones");

            migrationBuilder.DropTable(
                name: "TurnoMovimientos");

            migrationBuilder.DropTable(
                name: "TurnosCaja");

            migrationBuilder.DropIndex(
                name: "IX_PedidoVenta_TurnoId",
                table: "PedidoVenta");

            migrationBuilder.DropColumn(
                name: "TurnoId",
                table: "PedidoVenta");
        }
    }
}
