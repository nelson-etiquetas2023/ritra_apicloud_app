using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Migrations
{
    /// <inheritdoc />
    public partial class AddStatusProcesadoToCargasIniciales : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "CargasIniciales",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FechaProcesado",
                table: "CargasInicialesDetalles");

            migrationBuilder.DropColumn(
                name: "Procesado",
                table: "CargasInicialesDetalles");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "CargasIniciales");
        }
    }
}
