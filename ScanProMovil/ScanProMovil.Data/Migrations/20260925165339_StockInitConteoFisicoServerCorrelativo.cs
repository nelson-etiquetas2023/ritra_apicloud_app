using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ScanProMovil.Migrations
{
    /// <inheritdoc />
    public partial class StockInitConteoFisicoServerCorrelativo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // El conteo ya no guarda Doc. Teórico: la conciliación se decide en la web.
            migrationBuilder.DropColumn(
                name: "Document_Teorico",
                table: "StockInits");

            // Correlativo local para la identidad TMP-#### del conteo.
            migrationBuilder.CreateTable(
                name: "ConsecutivosStockInit",
                columns: table => new
                {
                    Tipo_Documento = table.Column<string>(type: "TEXT", nullable: false),
                    UltimoNumero = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConsecutivosStockInit", x => x.Tipo_Documento);
                });

            // Correlativo FIS-#### que asigna el servidor al crear el documento.
            migrationBuilder.AddColumn<string>(
                name: "NumeroServidor",
                table: "StockInits",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NumeroServidor",
                table: "StockInits");

            migrationBuilder.DropTable(
                name: "ConsecutivosStockInit");

            migrationBuilder.AddColumn<string>(
                name: "Document_Teorico",
                table: "StockInits",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }
    }
}
