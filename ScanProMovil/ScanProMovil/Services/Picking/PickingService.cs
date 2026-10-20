using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ScanProMovil.Data;
using ScanProMovil.Data.Entities;
using Shared.Dtos.Ventas;
using System.Text;
using System.Text.Json;
using System.Collections.ObjectModel;

namespace ScanProMovil.Services.Picking
{
    public class PickingService : IPickingService
    {
        private readonly IDbContextFactory<AppDbContext> _contextFactory;
        public IHttpClientFactory HttpFactory { get; set; }

        private static readonly JsonSerializerOptions jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true
        };

        public PickingService(IHttpClientFactory httpFactory, IDbContextFactory<AppDbContext> contextFactory)
        {
            HttpFactory = httpFactory;
            _contextFactory = contextFactory;
        }

        public async Task<List<PickingOrder>> GetPedidosParaPickingRemotoAsync()
        {
            try
            {
                var clientHttp = HttpFactory.CreateClient("scanpro");
                var response = await clientHttp.GetAsync("api/picking/pending");
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrWhiteSpace(json)) return new List<PickingOrder>();

                var pedidosApi = await JsonSerializer.DeserializeAsync<List<PedidoVenta>>(
                    new MemoryStream(Encoding.UTF8.GetBytes(json)), jsonOptions) ?? new List<PedidoVenta>();

                return MapearAPickingLocal(pedidosApi);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PickingService] Error obteniendo picking remoto: {ex.Message}");
                return new List<PickingOrder>();
            }
        }

        public async Task<List<PickingOrder>> GetPickingOrdersLocalAsync()
        {
            await using var _context = await _contextFactory.CreateDbContextAsync();
            return await _context.PickingOrders
                .Include(p => p.Items)
                .Where(p => p.Estado != 2)
                .OrderByDescending(p => p.PickingId)
                .ToListAsync();
        }

        public async Task<bool> SincronizarPickingDesdeApiAsync()
        {
            try
            {
                var pedidosRemotos = await GetPedidosParaPickingRemotoAsync();
                if (pedidosRemotos.Count == 0) return true;

                await using var _context = await _contextFactory.CreateDbContextAsync();

                // Obtener números ya sincronizados localmente
                var numerosLocales = await _context.PickingOrders
                    .Select(p => p.NumeroPedidoVenta)
                    .ToHashSetAsync();

                var nuevos = pedidosRemotos.Where(p => !numerosLocales.Contains(p.NumeroPedidoVenta)).ToList();

                foreach (var picking in nuevos)
                {
                    _context.PickingOrders.Add(picking);
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch (SqliteException ex)
            {
                Console.WriteLine($"[PickingService] Error SQLite sincronizando picking: {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PickingService] Error sincronizando picking: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> MarcarCompletadoAsync(int pickingId)
        {
            try
            {
                await using var _context = await _contextFactory.CreateDbContextAsync();
                var picking = await _context.PickingOrders
                    .FirstOrDefaultAsync(p => p.PickingId == pickingId);
                if (picking == null || picking.Estado == 2) return false;

                picking.Estado = 2;
                picking.FechaCompletado = DateTime.Now;
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PickingService] Error marcando completado: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> MarcarItemCompletadoAsync(int pickingId, int detailId)
        {
            try
            {
                await using var _context = await _contextFactory.CreateDbContextAsync();
                var item = await _context.PickingOrderItems
                    .FirstOrDefaultAsync(i => i.DetailId == detailId && i.PickingId == pickingId);
                if (item == null) return false;

                item.Completado = true;
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PickingService] Error marcando ítem {detailId}: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> CompletarOrdenRemotoAsync(int pickingId)
        {
            try
            {
                var clientHttp = HttpFactory.CreateClient("scanpro");
                var response = await clientHttp.PostAsync($"api/picking/complete/{pickingId}", null);
                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"[PickingService] API respondió {response.StatusCode} al completar picking {pickingId}");
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PickingService] Error completando orden remoto {pickingId}: {ex.Message}");
                return false;
            }
        }

        private static List<PickingOrder> MapearAPickingLocal(List<PedidoVenta> pedidosApi)
        {
            var resultado = new List<PickingOrder>();

            foreach (var p in pedidosApi)
            {
                var picking = new PickingOrder
                {
                    NumeroPedidoVenta = p.Numero,
                    Estado = 0, // Pendiente
                    FechaCreacion = p.FechaCreacion,
                    UsuarioAsignado = p.Vendedor,
                    Items = new ObservableCollection<PickingOrderDetail>()
                };

                foreach (var item in p.Items)
                {
                    picking.Items.Add(new PickingOrderDetail
                    {
                        ProductCode = item.Product_id,
                        ProductName = item.Product_name,
                        Cantidad = item.Cantidad,
                        Completado = false
                    });
                }

                resultado.Add(picking);
            }

            return resultado;
        }
    }
}
