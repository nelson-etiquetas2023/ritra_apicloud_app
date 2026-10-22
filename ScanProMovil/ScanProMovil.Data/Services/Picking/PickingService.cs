using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ScanProMovil.Data;
using ScanProMovil.Data.Entities;
using ScanProMovil.Data.Services;
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

                return PickingSincronizador.Mapear(remotos);
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

                var resultado = await PickingSincronizador.AplicarAsync(_context, remotos, idsRemotos);

                // Propaga al servidor las ordenes que aqui ya estaban completas.
                foreach (var local in resultado.PendientesDeCerrarRemoto)
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
            // El mapeo vive en el proyecto de datos para poder probarlo sin
            // dispositivo movil.
            return PickingSincronizador.Mapear(remotos);
        }
    }
}
