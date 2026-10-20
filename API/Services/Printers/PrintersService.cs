using API.Data;
using Microsoft.EntityFrameworkCore;
using Shared.Dtos;

namespace API.Services.Printers
{
    public class PrintersService(ApplicationDbContext context) : IPrintersService
    {
        private static string FormatCode(int number) => $"PRN-{number:D4}";

        private async Task<int> GetMaxCodeNumberAsync()
        {
            var codigos = await context.Printers
                .Where(p => p.Code != null && p.Code.StartsWith("PRN-"))
                .Select(p => p.Code)
                .ToListAsync();

            var max = 0;
            foreach (var codigo in codigos)
            {
                if (codigo.Length == 8 && int.TryParse(codigo.AsSpan(4), out var number) && number > max)
                    max = number;
            }
            return max;
        }

        public async Task<string> GetNextNumAsync()
        {
            return FormatCode(await GetMaxCodeNumberAsync() + 1);
        }

        public async Task<List<Printer>> GetAllAsync()
        {
            return await context.Printers
                .OrderBy(p => p.Name)
                .ToListAsync();
        }

        public async Task<Printer?> GetByIdAsync(int id)
        {
            return await context.Printers.FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<Printer?> CreateAsync(Printer printer)
        {
            if (printer == null || string.IsNullOrWhiteSpace(printer.Name)) return null;

            printer.Name = printer.Name.Trim();
            printer.Model = printer.Model?.Trim() ?? string.Empty;
            if (!PrinterConnection.All.Contains(printer.Connection))
                printer.Connection = PrinterConnection.Tcp;
            printer.Ip = printer.Ip?.Trim() ?? string.Empty;
            printer.Endpoint = printer.Endpoint?.Trim() ?? string.Empty;
            if (printer.Port <= 0) printer.Port = 9100;
            if (printer.Dpi is not (152 or 203 or 300 or 600)) printer.Dpi = 203;
            if (printer.AnchoMaxIn <= 0) printer.AnchoMaxIn = 4;
            printer.Id = 0;

            for (int attempt = 0; attempt < 3; attempt++)
            {
                printer.Code = FormatCode(await GetMaxCodeNumberAsync() + 1);
                context.Printers.Add(printer);
                try
                {
                    await context.SaveChangesAsync();
                    return printer;
                }
                catch (DbUpdateException)
                {
                    context.Entry(printer).State = EntityState.Detached;
                }
            }

            return null;
        }

        public async Task<Printer?> UpdateAsync(int id, Printer printer)
        {
            if (printer == null || string.IsNullOrWhiteSpace(printer.Name)) return null;

            var existing = await context.Printers.FirstOrDefaultAsync(p => p.Id == id);
            if (existing == null) return null;

            existing.Name = printer.Name.Trim();
            existing.Model = printer.Model?.Trim() ?? string.Empty;
            if (PrinterConnection.All.Contains(printer.Connection))
                existing.Connection = printer.Connection;
            existing.Ip = printer.Ip?.Trim() ?? string.Empty;
            existing.Endpoint = printer.Endpoint?.Trim() ?? string.Empty;
            if (printer.Port > 0) existing.Port = printer.Port;
            if (printer.Dpi is 152 or 203 or 300 or 600) existing.Dpi = printer.Dpi;
            if (printer.AnchoMaxIn > 0) existing.AnchoMaxIn = printer.AnchoMaxIn;
            existing.IsActive = printer.IsActive;

            await context.SaveChangesAsync();
            return existing;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var printer = await context.Printers.FirstOrDefaultAsync(p => p.Id == id);
            if (printer == null) return false;

            var enCola = await context.PrintJobs.AnyAsync(j =>
                j.PrinterId == id &&
                (j.Status == PrintJobStatus.Pending || j.Status == PrintJobStatus.Printing));
            if (enCola) return false;

            context.Printers.Remove(printer);
            await context.SaveChangesAsync();
            return true;
        }

        public async Task<(bool Ok, string? Error)> CalibrateAsync(int id)
        {
            var printer = await context.Printers.FirstOrDefaultAsync(p => p.Id == id);
            if (printer == null) return (false, $"Impresora {id} no encontrada.");
            if (!printer.IsActive) return (false, "La impresora está inactiva.");
            if (!PrinterConnection.EsRed(printer.Connection))
                return (false, $"La impresora {printer.Name} es {printer.Connection}: el calibrado gap se hace en el panel (FEED 2 parpadeos).");
            if (string.IsNullOrWhiteSpace(printer.Ip)) return (false, "La impresora no tiene IP.");

            try
            {
                using var client = new System.Net.Sockets.TcpClient();
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
                await client.ConnectAsync(printer.Ip, printer.Port, cts.Token);
                using var ns = client.GetStream();
                var zpl = "~JC\n^XA^MNW^JUS^XZ\n";
                var bytes = System.Text.Encoding.ASCII.GetBytes(zpl);
                await ns.WriteAsync(bytes, 0, bytes.Length, cts.Token);
                await ns.FlushAsync(cts.Token);
                return (true, null);
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }
    }
}
