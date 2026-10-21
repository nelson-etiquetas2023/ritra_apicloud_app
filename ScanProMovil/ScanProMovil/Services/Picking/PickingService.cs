using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ScanProMovil.Data;
using ScanProMovil.Data.Entities;
using Shared.Dtos.Ventas;
using System.Text;
using System.Text.Json;
using System.Collections.ObjectModel;
// Alias: la entidad local PickingOrder choca con la del DTO compartido.
using PickingPendienteDto = Shared.Dtos.Picking.PickingPendienteDto;

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

                var remotos = await JsonSerializer.DeserializeAsync<List<PickingPendienteDto>>(
                    new MemoryStream(Encoding.UTF8.GetBytes(json)), jsonOptions) ?? new List<PickingPendienteDto>();

                return MapearAPickingLocal(remotos);
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
                var remotos = await GetPedidosParaPickingRemotoAsync();
                if (remotos.Count == 0) return true;

                await using var _context = await _contextFactory.CreateDbContextAsync();

                var idsRemotos = remotos.Select(p => p.PickingId).ToList();

                var locales = await _context.PickingOrders
                    .Include(p => p.Items)
                    .Where(p => idsRemotos.Contains(p.PickingId))
                    .ToListAsync();

                var localesPorId = locales.ToDictionary(p => p.PickingId);

                // Ordenes completadas en el dispositivo que el servidor aun no
                // sabe: se cierran alli para que el pedido no vuelva a la lista.
                var pendientesDeCerrar = new List<PickingOrder>();

                foreach (var remoto in remotos)
                {
                    if (localesPorId.TryGetValue(remoto.PickingId, out var local))
                    {
                        // Ya existe: se actualiza el estado sin perder el
                        // avance de los items marcados en el dispositivo.
                        if (local.Estado != 2)
                        {
                            local.Estado = remoto.Estado;
                            local.UsuarioAsignado = remoto.UsuarioAsignado ?? local.UsuarioAsignado;
                            local.FechaCreacion = remoto.FechaCreacion;
                        }
                        else if (remoto.Estado != 2)
                        {
                            pendientesDeCerrar.Add(local);
                        }

                        // Items nuevos que todavia no estan en local.
                        var codigosLocales = local.Items.Select(i => i.ProductCode).ToHashSet();
                        foreach (var item in remoto.Items)
                        {
                            if (codigosLocales.Contains(item.ProductCode)) continue;

                            local.Items.Add(new PickingOrderDetail
                            {
                                PickingId = local.PickingId,
                                ProductCode = item.ProductCode,
                                ProductName = item.ProductName,
                                Cantidad = item.Cantidad,
                                Completado = false
                            });
                        }
                    }
                    else
                    {
                        _context.PickingOrders.Add(remoto);
                    }
                }

                await _context.SaveChangesAsync();

                // Propaga al servidor las ordenes que aqui ya estaban completas.
                foreach (var local in pendientesDeCerrar)
                {
                    await CompletarOrdenRemotoAsync(local.PickingId);
                }

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

        private static List<PickingOrder> MapearAPickingLocal(List<PickingPendienteDto> remotos)
        {
            var resultado = new List<PickingOrder>();

            foreach (var remoto in remotos)
            {
                var pedido = remoto.Pedido;

                var picking = new PickingOrder
                {
                    // El Id remoto es la clave primaria local: asi al completar
                    // la orden se puede cerrar en el servidor sin ambiguedad.
                    PickingId = remoto.PickingId,
                    NumeroPedidoVenta = remoto.NumeroPedidoVenta,
                    Estado = remoto.Estado,
                    FechaCreacion = remoto.FechaCreacion,
                    UsuarioAsignado = remoto.UsuarioAsignado ?? pedido.Vendedor,
                    FechaCompletado = remoto.FechaCompletado,
                    Items = new ObservableCollection<PickingOrderDetail>()
                };

                foreach (var item in pedido.Items)
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
