using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using ScanProMovil.Data;
using ScanProMovil.Services.Logging;
using ScanProMovil.Views;

namespace ScanProMovil
{
    public partial class App : Application
    {
        // CreateWindow puede invocarse más de una vez por proceso: migrar solo una vez.
        private static int _migrationsApplied;

        public App()
        {
            InitializeComponent();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            // Aplicar migraciones pendientes al arrancar
            ApplyMigrations();
            
            return new Window(new SplashPage());
        }

        private static void ApplyMigrations()
        {
            if (Interlocked.Exchange(ref _migrationsApplied, 1) == 1)
                return;

            try
            {
                var factory = MauiProgram.Services?.GetRequiredService<IDbContextFactory<AppDbContext>>();
                if (factory is not null)
                {
                    using var context = factory.CreateDbContext();

                    // EF Core 9+ toma un lock en __EFMigrationsLock durante la migración.
                    // Si el proceso murió con el lock puesto (cierre en caliente, ANR, kill
                    // del sistema), la fila queda huérfana y TODA migración posterior espera
                    // indefinidamente (app colgada en el splash). Ver
                    // https://aka.ms/efcore-docs-migrations-lock — "abandoned migration locks
                    // can block subsequent migrations". Al arrancar no puede existir otra
                    // migración en curso (proceso único, una sola vez por proceso), así que
                    // cualquier fila previa está abandonada por definición.
                    ReleaseAbandonedMigrationLock(context);

                    context.Database.Migrate();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error applying migrations: {ex.Message}");
                AppLog.Error("App.ApplyMigrations", ex);
            }
        }

        /// <summary>
        /// Elimina la fila abandonada de __EFMigrationsLock (solo en arranque en frío).
        /// Sin esto, una base con lock huérfano deja la app colgada en el splash
        /// reintentando el INSERT OR IGNORE con backoff exponencial para siempre.
        /// </summary>
        private static void ReleaseAbandonedMigrationLock(AppDbContext context)
        {
            try
            {
                var conn = context.Database.GetDbConnection();
                var reopen = conn.State != System.Data.ConnectionState.Open;
                if (reopen)
                    conn.Open();
                try
                {
                    using var check = conn.CreateCommand();
                    check.CommandText =
                        "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='__EFMigrationsLock'";
                    var exists = Convert.ToInt32(check.ExecuteScalar()) > 0;
                    if (!exists)
                        return; // Base nueva: nada que limpiar.
                }
                finally
                {
                    if (reopen)
                        conn.Close();
                }

                var removed = context.Database.ExecuteSqlRaw("DELETE FROM \"__EFMigrationsLock\"");
                if (removed > 0)
                {
                    AppLog.Warn("App.MigrationLock",
                        $"Lock abandonado de __EFMigrationsLock eliminado ({removed} fila(s)). " +
                        "Sin esta limpieza la app quedaba colgada en el splash.");
                }
            }
            catch (Exception ex)
            {
                // Nunca tumbar el arranque por la limpieza: si falla, decide Migrate().
                AppLog.Warn("App.MigrationLock", $"No se pudo verificar/limpiar __EFMigrationsLock: {ex.Message}");
            }
        }
    }
}