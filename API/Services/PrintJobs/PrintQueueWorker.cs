using API.Data;
using Microsoft.EntityFrameworkCore;
using Shared.Dtos;

namespace API.Services.PrintJobs
{
    public class PrintQueueWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<PrintQueueWorker> logger) : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                await ResetStalePrintingAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                // La tabla puede no existir aún si la migración no aplicó: no tumbar el host.
                logger.LogWarning(ex, "PrintQueueWorker: no se pudo resetear trabajos stale (¿migración pendiente?)");
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessNextAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error en PrintQueueWorker");
                }

                await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
            }
        }

        private async Task ResetStalePrintingAsync(CancellationToken ct)
        {
            using var scope = scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var stale = await context.PrintJobs
                .Where(j => j.Status == PrintJobStatus.Printing)
                .ToListAsync(ct);
            foreach (var job in stale) job.Status = PrintJobStatus.Pending;
            if (stale.Count > 0) await context.SaveChangesAsync(ct);
        }

        private async Task ProcessNextAsync(CancellationToken ct)
        {
            using var scope = scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var job = await context.PrintJobs
                .Where(j => j.Status == PrintJobStatus.Pending)
                .OrderBy(j => j.Orden)
                .ThenBy(j => j.CreatedAt)
                .FirstOrDefaultAsync(ct);
            if (job == null) return;

            var printer = await context.Printers.FirstOrDefaultAsync(p => p.Id == job.PrinterId, ct);
            if (printer == null || !printer.IsActive)
            {
                job.Status = PrintJobStatus.Failed;
                job.Error = $"Impresora {job.PrinterId} no disponible.";
                await context.SaveChangesAsync(ct);
                return;
            }
            if (!PrinterConnection.EsRed(printer.Connection))
            {
                job.Status = PrintJobStatus.Failed;
                job.Error = $"La impresora {printer.Name} es {printer.Connection}: solo imprime directo desde el navegador.";
                await context.SaveChangesAsync(ct);
                return;
            }

            job.Status = PrintJobStatus.Printing;
            await context.SaveChangesAsync(ct);

            try
            {
                using var client = new System.Net.Sockets.TcpClient();
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                cts.CancelAfter(TimeSpan.FromSeconds(15));
                await client.ConnectAsync(printer.Ip, printer.Port, cts.Token);
                using var ns = client.GetStream();
                var bytes = System.Text.Encoding.ASCII.GetBytes(job.ZplFinal);
                await ns.WriteAsync(bytes, 0, bytes.Length, cts.Token);
                await ns.FlushAsync(cts.Token);

                job.Status = PrintJobStatus.Done;
                job.PrintedAt = DateTime.UtcNow;
                job.Error = string.Empty;
            }
            catch (Exception ex)
            {
                job.Status = PrintJobStatus.Failed;
                job.Error = ex.Message;
                logger.LogError(ex, "Fallo impresión trabajo {JobId} en {Ip}:{Port}",
                    job.Id, printer.Ip, printer.Port);
            }

            await context.SaveChangesAsync(ct);
        }
    }
}
