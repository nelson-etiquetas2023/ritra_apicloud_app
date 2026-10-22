using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ScanProMovil.Migrations
{
    /// <inheritdoc />
    public partial class LimpiarPickingLocal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // La clave primaria local ahora es el Id de la orden de picking en
            // el servidor. Las filas que se tenian con Id generado local ya
            // no significan nada, asi que se limpian: el dispositivo vuelve a
            // descargarlas al sincronizar.
            migrationBuilder.Sql("DELETE FROM PickingOrderItems;");
            migrationBuilder.Sql("DELETE FROM PickingOrders;");

            migrationBuilder.AlterColumn<int>(
                name: "PickingId",
                table: "PickingOrders",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER")
                .OldAnnotation("Sqlite:Autoincrement", true);

            migrationBuilder.AddColumn<string>(
                name: "ClienteNombre",
                table: "PickingOrders",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ClienteNombre",
                table: "PickingOrders");

            migrationBuilder.AlterColumn<int>(
                name: "PickingId",
                table: "PickingOrders",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER")
                .Annotation("Sqlite:Autoincrement", true);
        }
    }
}
