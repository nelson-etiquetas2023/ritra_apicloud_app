using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ScanProMovil.Migrations
{
    /// <inheritdoc />
    [Migration("20261006120000_FixConsecutivosStockInitIfExists")]
    public partial class FixConsecutivosStockInitIfExists : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // SQLite: CREATE TABLE IF NOT EXISTS para evitar error "table already exists"
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS ""ConsecutivosStockInit"" (
                    ""Tipo_Documento"" TEXT NOT NULL PRIMARY KEY,
                    ""UltimoNumero"" INTEGER NOT NULL DEFAULT 0
                );
            ");

            // Asegurar que existe el registro base para 'FIS'
            migrationBuilder.Sql(@"
                INSERT OR IGNORE INTO ""ConsecutivosStockInit"" (""Tipo_Documento"", ""UltimoNumero"")
                VALUES ('FIS', 0);
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No hacer nada en Down - la tabla puede ser necesaria
        }
    }
}