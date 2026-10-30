using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ScanProMovil.Migrations
{
    /// <inheritdoc />
    public partial class PickingPickeadoPorLinea : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Completado",
                table: "PickingOrderItems",
                newName: "Pickeado");

            // Los datos viejos eran un bool (0/1): una linea marcada completa
            // equivale a Pickeado = Cantidad (se pickeo todo). El resto queda 0.
            migrationBuilder.Sql("UPDATE \"PickingOrderItems\" SET \"Pickeado\" = \"Cantidad\" WHERE \"Pickeado\" <> 0;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Pickeado",
                table: "PickingOrderItems",
                newName: "Completado");

            // Vuelta al bool: pickeada completa = 1, lo demas = 0.
            migrationBuilder.Sql("UPDATE \"PickingOrderItems\" SET \"Completado\" = (CASE WHEN \"Cantidad\" > 0 AND \"Completado\" >= \"Cantidad\" THEN 1 ELSE 0 END);");
        }
    }
}
