using Microsoft.Data.SqlClient;

// ============ sqltool: ejecutar SQL contra SQL Server (RitramaCloud) ============
// Sin sqlcmd en esta máquina: este mini cliente ejecuta el SQL que llegue por
// argumento (o por stdin) sobre la misma cadena del API ("SERVIDOR-ETIQUETA").
//
//   dotnet run --project repro/sqltool -- SELECT @@VERSION
//   Get-Content script.sql -Raw | dotnet run --project repro/sqltool

const string connectionString =
    "Data Source=192.168.10.10; Initial Catalog=RitramaCloud;" +
    "User Id=Npino;Password=Jossycar5%;Encrypt=True;TrustServerCertificate=True;Connect Timeout=15;";

// SQL por argumentos; si no hay, leer de stdin (pipe).
var sql = string.Join(" ", args).Trim();
if (sql.Length == 0)
{
    Console.Error.WriteLine("Esperando SQL por stdin (Ctrl+Z + Enter para terminar)...");
    sql = await Console.In.ReadToEndAsync();
}

if (string.IsNullOrWhiteSpace(sql))
{
    Console.Error.WriteLine("No hay SQL que ejecutar.");
    return 2;
}

await using var connection = new SqlConnection(connectionString);
try
{
    await connection.OpenAsync();
    Console.WriteLine($"Conectado: {connection.Database} en {connection.DataSource}");
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Fallo la conexion: {ex.Message}");
    return 1;
}

await using var batch = new SqlCommand(sql, connection) { CommandTimeout = 120 };

try
{
    using var reader = await batch.ExecuteReaderAsync();
    var any = false;
    do
    {
        var rows = 0;
        while (reader.FieldCount > 0 && await reader.ReadAsync())
        {
            any = true;
            rows++;
            var values = new object[reader.FieldCount];
            reader.GetValues(values);
            Console.WriteLine(string.Join(" | ", values.Select(v => v?.ToString() ?? "NULL")));
        }
        if (rows > 0)
            Console.WriteLine($"({rows} fila(s))");
        else if (reader.FieldCount > 0)
            Console.WriteLine("(0 filas)");
        else if (reader.RecordsAffected >= 0)
            Console.WriteLine($"Listo. Filas afectadas: {reader.RecordsAffected}");
    } while (await reader.NextResultAsync());
    if (!any)
        Console.WriteLine("(sin resultados)");
    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Error SQL: {ex.Message}");
    return 1;
}
