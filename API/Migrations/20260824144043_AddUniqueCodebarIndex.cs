using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueCodebarIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Limpia duplicados de Codebar (ej. placeholder "1") para que el índice único filtrado no falle
            migrationBuilder.Sql(@"
                WITH keep AS (
                    SELECT Codebar, MIN(Product_id) AS keep_id
                    FROM Productos
                    WHERE Codebar IS NOT NULL AND Codebar <> ''
                    GROUP BY Codebar
                )
                UPDATE p SET Codebar = ''
                FROM Productos p
                JOIN keep k ON p.Codebar = k.Codebar
                WHERE p.Product_id <> k.keep_id;
            ");

            migrationBuilder.AlterColumn<string>(
                name: "Codebar",
                table: "Productos",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateIndex(
                name: "IX_Productos_Codebar",
                table: "Productos",
                column: "Codebar",
                unique: true,
                filter: "[Codebar] IS NOT NULL AND [Codebar] <> ''");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Productos_Codebar",
                table: "Productos");

            migrationBuilder.AlterColumn<string>(
                name: "Codebar",
                table: "Productos",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");
        }
    }
}
