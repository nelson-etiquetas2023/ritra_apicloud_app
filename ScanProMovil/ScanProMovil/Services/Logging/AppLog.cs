using System.Diagnostics;

namespace ScanProMovil.Services.Logging
{
    /// <summary>
    /// Logger simple persistente a archivo. Debug.WriteLine se pierde en Release;
    /// aquí los errores quedan en &lt;AppDataDirectory&gt;/logs/app-YYYYMMDD.log
    /// para diagnóstico en campo (el usuario puede enviar el archivo por WhatsApp/correo).
    /// </summary>
    public static class AppLog
    {
        private static readonly object _lock = new();
        private const long MaxFileBytes = 5 * 1024 * 1024; // 5 MB por día

        public static string LogDirectory => Path.Combine(FileSystem.AppDataDirectory, "logs");

        public static void Error(string contexto, Exception ex) => Write("ERROR", contexto, ex.ToString());
        public static void Error(string contexto, string mensaje) => Write("ERROR", contexto, mensaje);
        public static void Warn(string contexto, string mensaje) => Write("WARN", contexto, mensaje);
        public static void Info(string contexto, string mensaje) => Write("INFO", contexto, mensaje);

        private static void Write(string nivel, string contexto, string mensaje)
        {
            var linea = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{nivel}] {contexto}: {mensaje}";
            Debug.WriteLine(linea);

            try
            {
                lock (_lock)
                {
                    Directory.CreateDirectory(LogDirectory);
                    var file = Path.Combine(LogDirectory, $"app-{DateTime.Now:yyyyMMdd}.log");

                    if (File.Exists(file) && new FileInfo(file).Length > MaxFileBytes)
                        File.Delete(file); // rotación simple: un archivo por día, máx. 5 MB

                    File.AppendAllText(file, linea + Environment.NewLine);
                }
            }
            catch
            {
                // El logging nunca debe tumbar la aplicación.
            }
        }
    }
}
