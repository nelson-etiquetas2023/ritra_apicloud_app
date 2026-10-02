using Microsoft.EntityFrameworkCore;
using ScanProMovil.Data;
using ScanProMovil.Data.Entities;
using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;

namespace ScanProMovil.Services.Compras
{
    public class ComprasService : IComprasService
    {
        // Factory: cada operación crea su propio DbContext (EF Core no es thread-safe).
        private readonly IDbContextFactory<AppDbContext> _contextFactory;
        public IHttpClientFactory httpFactory { get; set; }
        private static readonly JsonSerializerOptions jsonOptions = new JsonSerializerOptions()
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true
        };

        public ComprasService(IHttpClientFactory HttpFactory, IDbContextFactory<AppDbContext> contextFactory)
        {
            this.httpFactory = HttpFactory;
            _contextFactory = contextFactory;
        }

        public async Task<bool> SaveOrdersLocalSqliteAsync(OrdenCompra order)
        {
            try
            {
                await using var _context = await _contextFactory.CreateDbContextAsync();
                _context.PurchaseOrders.Add(order);
                await _context.SaveChangesAsync();
                _context.Entry(order).State = EntityState.Detached;
                Debug.WriteLine($"Orden de compra {order.Numero} guardada en sqlite");

                await UpdateConsecutivoAsync(order);

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("error sqlite:" + ex.Message);
                return false;
            }
        }

        private async Task UpdateConsecutivoAsync(OrdenCompra order)
        {
            var tipo = string.IsNullOrWhiteSpace(order.Tipo_Documento) ? "OC" : order.Tipo_Documento;
            var numeroUsado = int.TryParse(order.Numero, out var value) ? value : 0;

            await using var _context = await _contextFactory.CreateDbContextAsync();
            var consecutivo = await _context.Consecutivos
                .FirstOrDefaultAsync(c => c.Tipo_Documento == tipo);

            if (consecutivo is null)
            {
                consecutivo = new ConsecutivoCompra
                {
                    Tipo_Documento = tipo,
                    UltimoNumero = numeroUsado
                };
                _context.Consecutivos.Add(consecutivo);
            }
            else if (numeroUsado > consecutivo.UltimoNumero)
            {
                consecutivo.UltimoNumero = numeroUsado;
            }

            await _context.SaveChangesAsync();
        }

        public async Task<List<OrdenCompra>> GetOrdersLocalSqliteAsync()
        {
            await using var _context = await _contextFactory.CreateDbContextAsync();
            return await _context.PurchaseOrders
                .AsNoTracking()
                .Include(o => o.Items)
                .OrderByDescending(o => o.FechaCreacion)
                .ThenByDescending(o => o.Numero)
                .ToListAsync();
        }

        public async Task<string> GetNextOrderNumberAsync()
        {
            const string tipo = "OC";
            await using var _context = await _contextFactory.CreateDbContextAsync();
            var consecutivo = await _context.Consecutivos
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Tipo_Documento == tipo);

            return ((consecutivo?.UltimoNumero ?? 0) + 1).ToString("D4");
        }

        public Task<List<OrdenCompra>> getOrders() => GetOrdersLocalSqliteAsync();

        public async Task<OrdenCompra> getOrderById(string OrderId)
        {
            await using var _context = await _contextFactory.CreateDbContextAsync();
            return await _context.PurchaseOrders.AsNoTracking()
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Numero == OrderId) ?? new OrdenCompra();
        }

        public async Task<OrdenCompra> UpdateOrder(OrdenCompra order)
        {
            await using var _context = await _contextFactory.CreateDbContextAsync();
            _context.PurchaseOrders.Update(order);
            await _context.SaveChangesAsync();
            return order;
        }

        public async Task<bool> DeleteOrder(string OrderId)
        {
            try
            {
                await using var _context = await _contextFactory.CreateDbContextAsync();
                var order = await _context.PurchaseOrders
                    .FirstOrDefaultAsync(o => o.Numero == OrderId);
                if (order is null) return false;

                _context.PurchaseOrders.Remove(order);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("error al borrar orden de compra: " + ex.Message);
                return false;
            }
        }

        public async Task<bool> MarkOrderSynchronizedAsync(string numero)
        {
            try
            {
                await using var _context = await _contextFactory.CreateDbContextAsync();
                var order = await _context.PurchaseOrders
                    .FirstOrDefaultAsync(o => o.Numero == numero);
                if (order is null) return false;

                order.Status = 2;
                order.Sincro = true;
                await _context.SaveChangesAsync();
                Debug.WriteLine($"Orden de compra {numero} marcada como sincronizada");
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("error al marcar orden como sincronizada: " + ex.Message);
                return false;
            }
        }

        public async Task<bool> DeactivateOrder(string Orderid)
        {
            try
            {
                await using var _context = await _contextFactory.CreateDbContextAsync();
                var order = await _context.PurchaseOrders
                    .FirstOrDefaultAsync(o => o.Numero == Orderid);
                if (order is null) return false;

                order.Status = 3; // Cerrado
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("error al desactivar orden de compra: " + ex.Message);
                return false;
            }
        }

        public async Task<bool> SendPurchaseOrder(OrdenCompra order, CancellationToken cancellationToken = default)
        {
            // NUEVO: Validación del parámetro
            ArgumentNullException.ThrowIfNull(order);

            var url = $"api/ordencompra/addorder";
            var clienteHttp = httpFactory.CreateClient("scanpro");
            var responseServer = await clienteHttp.PostAsJsonAsync(url, order,
                jsonOptions, cancellationToken);

            if (responseServer.IsSuccessStatusCode)
            {
                return true;
            }

            var error = await responseServer.Content.ReadAsStringAsync(cancellationToken);
            Debug.WriteLine($"HTTP {(int)responseServer.StatusCode}: {error}");
            throw new ApplicationException(
                $"El servidor respondió un error HTTP {(int)responseServer.StatusCode}: {error}");
        }
    }
}
