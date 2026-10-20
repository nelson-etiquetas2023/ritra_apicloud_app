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
            // Usar SQL raw con IF NOT EXISTS para evitar error si la tabla ya existe
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS ""ConsecutivosStockInit"" (
                    ""Tipo_Documento"" TEXT NOT NULL PRIMARY KEY,
                    ""UltimoNumero"" INTEGER NOT NULL DEFAULT 0
                );
            ");

            // Asegurar registro base 'FIS'
            migrationBuilder.Sql(@"
                INSERT OR IGNORE INTO ""ConsecutivosStockInit"" (""Tipo_Documento"", ""UltimoNumero"")
                VALUES ('FIS', 0);
            ");

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
