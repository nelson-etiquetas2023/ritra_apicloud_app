using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Migrations
{
    /// <summary>
    /// Renombra las dos tablas maestras de inventario inicial para que el nombre
    /// refleje donde se trabaja cada una:
    ///
    ///   Teorico -> InventarioTeorico / InventarioTeoricoDetalles  (web, import Excel)
    ///   Fisico  -> InventarioFisico  / InventarioFisicoDetalles   (movil, conteo en campo)
    ///
    /// Se usa sp_rename en lugar de DropTable/CreateTable para conservar los datos
    /// ya capturados. Las llaves foraneas tambien cambian de nombre
    /// (InicialId -> InventarioTeoricoId, DocumentoFisicoId -> InventarioFisicoId).
    /// </summary>
    public partial class RenombrarTablasInventarioInicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Tablas maestras y detalle
            migrationBuilder.Sql("EXEC sp_rename 'dbo.CargasIniciales', 'InventarioTeorico';");
            migrationBuilder.Sql("EXEC sp_rename 'dbo.CargasInicialesDetalles', 'InventarioTeoricoDetalles';");
            migrationBuilder.Sql("EXEC sp_rename 'dbo.DocumentosFisicos', 'InventarioFisico';");
            migrationBuilder.Sql("EXEC sp_rename 'dbo.DocumentosFisicosDetalles', 'InventarioFisicoDetalles';");

            // Columnas de llave foranea
            migrationBuilder.Sql("EXEC sp_rename 'dbo.InventarioTeoricoDetalles.InicialId', 'InventarioTeoricoId', 'COLUMN';");
            migrationBuilder.Sql("EXEC sp_rename 'dbo.InventarioFisicoDetalles.DocumentoFisicoId', 'InventarioFisicoId', 'COLUMN';");

            // Claves primarias
            migrationBuilder.Sql("EXEC sp_rename 'dbo.PK_CargasIniciales', 'PK_InventarioTeorico';");
            migrationBuilder.Sql("EXEC sp_rename 'dbo.PK_CargasInicialesDetalles', 'PK_InventarioTeoricoDetalles';");
            migrationBuilder.Sql("EXEC sp_rename 'dbo.PK_DocumentosFisicos', 'PK_InventarioFisico';");
            migrationBuilder.Sql("EXEC sp_rename 'dbo.PK_DocumentosFisicosDetalles', 'PK_InventarioFisicoDetalles';");

            // Llaves foraneas
            migrationBuilder.Sql("EXEC sp_rename 'dbo.FK_CargasInicialesDetalles_CargasIniciales_InicialId', 'FK_InventarioTeoricoDetalles_InventarioTeorico_InventarioTeoricoId';");
            migrationBuilder.Sql("EXEC sp_rename 'dbo.FK_DocumentosFisicosDetalles_DocumentosFisicos_DocumentoFisicoId', 'FK_InventarioFisicoDetalles_InventarioFisico_InventarioFisicoId';");

            // Indices: sp_rename no admite el tipo INDEX sobre estos objetos
            // (error 15248), asi que se recrean con el nombre nuevo.
            migrationBuilder.Sql("DROP INDEX [IX_CargasIniciales_Numero] ON [dbo].[InventarioTeorico];");
            migrationBuilder.Sql("CREATE UNIQUE INDEX [IX_InventarioTeorico_Numero] ON [dbo].[InventarioTeorico] ([Numero]);");

            migrationBuilder.Sql("DROP INDEX [IX_CargasInicialesDetalles_InicialId] ON [dbo].[InventarioTeoricoDetalles];");
            migrationBuilder.Sql("CREATE INDEX [IX_InventarioTeoricoDetalles_InventarioTeoricoId] ON [dbo].[InventarioTeoricoDetalles] ([InventarioTeoricoId]);");

            migrationBuilder.Sql("DROP INDEX [IX_DocumentosFisicos_Numero] ON [dbo].[InventarioFisico];");
            migrationBuilder.Sql("CREATE UNIQUE INDEX [IX_InventarioFisico_Numero] ON [dbo].[InventarioFisico] ([Numero]);");

            migrationBuilder.Sql("DROP INDEX [IX_DocumentosFisicosDetalles_DocumentoFisicoId] ON [dbo].[InventarioFisicoDetalles];");
            migrationBuilder.Sql("CREATE INDEX [IX_InventarioFisicoDetalles_InventarioFisicoId] ON [dbo].[InventarioFisicoDetalles] ([InventarioFisicoId]);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX [IX_InventarioFisicoDetalles_InventarioFisicoId] ON [dbo].[InventarioFisicoDetalles];");
            migrationBuilder.Sql("CREATE INDEX [IX_DocumentosFisicosDetalles_DocumentoFisicoId] ON [dbo].[InventarioFisicoDetalles] ([InventarioFisicoId]);");

            migrationBuilder.Sql("DROP INDEX [IX_InventarioFisico_Numero] ON [dbo].[InventarioFisico];");
            migrationBuilder.Sql("CREATE UNIQUE INDEX [IX_DocumentosFisicos_Numero] ON [dbo].[InventarioFisico] ([Numero]);");

            migrationBuilder.Sql("DROP INDEX [IX_InventarioTeoricoDetalles_InventarioTeoricoId] ON [dbo].[InventarioTeoricoDetalles];");
            migrationBuilder.Sql("CREATE INDEX [IX_CargasInicialesDetalles_InicialId] ON [dbo].[InventarioTeoricoDetalles] ([InventarioTeoricoId]);");

            migrationBuilder.Sql("DROP INDEX [IX_InventarioTeorico_Numero] ON [dbo].[InventarioTeorico];");
            migrationBuilder.Sql("CREATE UNIQUE INDEX [IX_CargasIniciales_Numero] ON [dbo].[InventarioTeorico] ([Numero]);");

            migrationBuilder.Sql("EXEC sp_rename 'dbo.FK_InventarioFisicoDetalles_InventarioFisico_InventarioFisicoId', 'FK_DocumentosFisicosDetalles_DocumentosFisicos_DocumentoFisicoId';");
            migrationBuilder.Sql("EXEC sp_rename 'dbo.FK_InventarioTeoricoDetalles_InventarioTeorico_InventarioTeoricoId', 'FK_CargasInicialesDetalles_CargasIniciales_InicialId';");

            migrationBuilder.Sql("EXEC sp_rename 'dbo.PK_InventarioFisicoDetalles', 'PK_DocumentosFisicosDetalles';");
            migrationBuilder.Sql("EXEC sp_rename 'dbo.PK_InventarioFisico', 'PK_DocumentosFisicos';");
            migrationBuilder.Sql("EXEC sp_rename 'dbo.PK_InventarioTeoricoDetalles', 'PK_CargasInicialesDetalles';");
            migrationBuilder.Sql("EXEC sp_rename 'dbo.PK_InventarioTeorico', 'PK_CargasIniciales';");

            migrationBuilder.Sql("EXEC sp_rename 'dbo.InventarioFisicoDetalles.InventarioFisicoId', 'DocumentoFisicoId', 'COLUMN';");
            migrationBuilder.Sql("EXEC sp_rename 'dbo.InventarioTeoricoDetalles.InventarioTeoricoId', 'InicialId', 'COLUMN';");

            migrationBuilder.Sql("EXEC sp_rename 'dbo.InventarioFisicoDetalles', 'DocumentosFisicosDetalles';");
            migrationBuilder.Sql("EXEC sp_rename 'dbo.InventarioFisico', 'DocumentosFisicos';");
            migrationBuilder.Sql("EXEC sp_rename 'dbo.InventarioTeoricoDetalles', 'CargasInicialesDetalles';");
            migrationBuilder.Sql("EXEC sp_rename 'dbo.InventarioTeorico', 'CargasIniciales';");
        }
    }
}
