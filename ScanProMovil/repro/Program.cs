using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ScanProMovil.Data;
using ScanProMovil.Data.Services;
using ScanProMovil.Services.Picking;

// ============ REPRO 2: sincronizacion completo contra el API REAL ============
// pipeline identico al del dispositivo: fetch real (HTTP) -> AplicarAsync
// (merge SQLite) -> GetPickingOrdersLocalAsync (lo que ve la pantalla).

var dbPath = Path.Combine(Path.GetTempPath(), $"repro-picking-sync-{Guid.NewGuid():N}.db");

var options = new DbContextOptionsBuilder<AppDbContext>()
    .UseSqlite($"Data Source={dbPath};Foreign Keys=True;Cache=Shared")
    .Options;

var realFactory = new FactoryDbContext(options);
await using (var ctx = realFactory.CreateDbContext())
{
    ReleaseLock(ctx);
    ctx.Database.Migrate();
}

var pickingService = new PickingService(new RealHttpFactory(), realFactory);

// 1) Fetch REAL contra el API de la red (igual que el dispositivo)
Console.WriteLine("=== 1) FETCH API api/picking/pending ===");
var remotos = await pickingService.GetPedidosParaPickingRemotoAsync();
Console.WriteLine($"Pedidos deserializados: {remotos.Count}");
foreach (var r in remotos)
{
    Console.WriteLine($"   Id={r.PickingId}, Num={r.NumeroPedidoVenta}, Estado={r.Estado}, Cliente={r.ClienteNombre}, Items={r.Items.Count}");
    foreach (var i in r.Items)
        Console.WriteLine($"      Item: Code={i.ProductCode}, Name={i.ProductName}, Cantidad={i.Cantidad}");
}

if (remotos.Count == 0)
{
    Console.WriteLine(">>> El API no devolvio pedidos o la deserializada perdio datos: AQUI ESTA EL FALLO.");
    return;
}

// 2) Merge local (misma ruta que SincronizarPickingDesdeApiAsync)
Console.WriteLine("\n=== 2) MERGE LOCAL ===");
await using (var ctx = realFactory.CreateDbContext())
{
    var ids = remotos.Select(p => p.PickingId).ToList();
    var res = await PickingSincronizador.AplicarAsync(ctx, remotos, ids);
    Console.WriteLine($"Resultado: insertadas={res.Insertadas}, actualizadas={res.Actualizadas}, cerradasLocalmente={res.CerradasLocalmente}, pendientesCerrarRemoto={res.PendientesDeCerrarRemoto.Count}");
    foreach (var p in res.PendientesDeCerrarRemoto)
        Console.WriteLine($"   !! cerraria en remoto: Id={p.PickingId} ({p.NumeroPedidoVenta})");
}

// 3) Lectura local (lo que muestra el main)
Console.WriteLine("\n=== 3) LECTURA LOCAL (pantalla) ===");
var locales = await pickingService.GetPickingOrdersLocalAsync();
Console.WriteLine($"Locales pendientes (Estado != 2): {locales.Count}");
foreach (var o in locales)
    Console.WriteLine($"   Id={o.PickingId}, Num={o.NumeroPedidoVenta}, Estado={o.Estado}, Items={o.Items.Count}, Progreso={o.ProgresoTexto}");

try { File.Delete(dbPath); } catch { }
return;

static void ReleaseLock(AppDbContext context)
{
    try { context.Database.ExecuteSqlRaw("DELETE FROM \"__EFMigrationsLock\""); }
    catch { /* base nueva */ }
}

// HttpClient real con la base del API en la red (misma que usa el dispositivo).
class RealHttpFactory : IHttpClientFactory
{
    public HttpClient CreateClient(string name)
    {
        var client = new HttpClient { BaseAddress = new Uri("http://192.168.10.10:8080"), Timeout = TimeSpan.FromSeconds(8) };
        client.DefaultRequestHeaders.Add("User-Agent", "MauiApp");
        client.DefaultRequestHeaders.ConnectionClose = false;
        return client;
    }
}

class FactoryDbContext(DbContextOptions<AppDbContext> options) : IDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext() => new(options);
    public ValueTask<AppDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
        => new(CreateDbContext());
}
