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
            var tipo = string.IsNullOrWhiteSpace(order.Tipo_Documento) ? "PO" : order.Tipo_Documento;
            var numeroLimpio = order.Numero?.StartsWith("PO-", StringComparison.OrdinalIgnoreCase) == true
    ? order.Numero.Substring(3)
    : order.Numero;
            var numeroUsado = int.TryParse(numeroLimpio, out var value) ? value : 0;

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

        public async Task<List<Shared.Dtos.Compras.OrdenCompra>> GetOrdenesParaReceiveRemotoAsync()
        {
            try
            {
                var client = httpFactory.CreateClient("scanpro");
                var response = await client.GetAsync("api/ordencompra/getorders");
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrWhiteSpace(json)) return new();
                var remotos = JsonSerializer.Deserialize<List<Shared.Dtos.Compras.OrdenCompra>>(json, jsonOptions) ?? new();
                return remotos.Where(o => o.Status == 2).ToList();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error obteniendo compras remotas: {ex.Message}");
                return new();
            }
        }

        public async Task<bool> SincronizarComprasDesdeApiAsync()
        {
            try
            {
                var remotos = await GetOrdenesParaReceiveRemotoAsync();
                if (remotos.Count == 0) return true;

                await using var ctx = await _contextFactory.CreateDbContextAsync();
                foreach (var remoto in remotos)
                {
                    var local = await ctx.PurchaseOrders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Numero == remoto.Numero);
                    if (local == null)
                    {
                        var nuevo = new OrdenCompra
                        {
                            Numero = remoto.Numero,
                            Fecha = remoto.Fecha,
                            FechaCreacion = remoto.FechaCreacion == default ? DateTime.Now : remoto.FechaCreacion,
                            Description = remoto.Description,
                            Comentario = remoto.Comentario,
                            Status = remoto.Status,
                            Sincro = remoto.Sincro,
                            Tipo_Documento = remoto.Tipo_Documento,
                            Subtotal = remoto.Subtotal,
                            Impuesto = remoto.Impuesto,
                            Total = remoto.Total,
                            Supply_Id = remoto.Supply_Id,
                            Supply_Name = remoto.Supply_Name,
                            Reference = remoto.Reference,
                            Prioridad = remoto.Prioridad,
                            UserName = remoto.UserName,
                            UserEmail = remoto.UserEmail,
                            UserRole = remoto.UserRole,
                            DeviceCode = remoto.DeviceCode,
                            DeviceName = remoto.DeviceName,
                            WarehouseName = remoto.WarehouseName,
                            Items = remoto.Items?.Select(i => new DetalleCompra
                            {
                                Id = 0,
                                Numero = i.Numero,
                                Product_id = i.Product_id,
                                Product_name = i.Product_name,
                                Cantidad = i.Cantidad,
                                Costo = i.Costo,
                                Subtotal = i.Subtotal,
                                Comentario = i.Comentario,
                                Procesado = i.Procesado,
                                FechaProcesado = i.FechaProcesado
                            }).ToList() ?? new()
                        };
                        ctx.PurchaseOrders.Add(nuevo);
                    }
                    else
                    {
                        local.Status = remoto.Status;
                        local.Sincro = remoto.Sincro;
                        local.Supply_Name = remoto.Supply_Name;
                        local.Total = remoto.Total;
                    }
                }
                await ctx.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error sincronizando compras: {ex.Message}");
                return false;
            }
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
            const string tipo = "PO";
            await using var _context = await _contextFactory.CreateDbContextAsync();
            var consecutivo = await _context.Consecutivos
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Tipo_Documento == tipo);

            return $"PO-{((consecutivo?.UltimoNumero ?? 0) + 1):D4}";
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

        public async Task<Shared.Dtos.Compras.ProcesarOrdenResult> ReceiveOrderAsync(string numero)
        {
            var result = new Shared.Dtos.Compras.ProcesarOrdenResult { Success = false, Message = "" };
            try
            {
                if (string.IsNullOrWhiteSpace(numero))
                {
                    result.Message = "Numero de orden vacio.";
                    return result;
                }

                var clienteHttp = httpFactory.CreateClient("scanpro");
                var response = await clienteHttp.PostAsync($"api/inventario/process-compra/{numero}", null);

                var json = await response.Content.ReadAsStringAsync();
                if (response.IsSuccessStatusCode)
                {
                    result = JsonSerializer.Deserialize<Shared.Dtos.Compras.ProcesarOrdenResult>(json, jsonOptions)
                             ?? new Shared.Dtos.Compras.ProcesarOrdenResult { Success = true, Message = "Procesado." };
                }
                else
                {
                    result.Success = false;
                    result.Message = $"HTTP {(int)response.StatusCode}: {json}";
                    Debug.WriteLine($"ReceiveOrder {numero} error: {result.Message}");
                }
                return result;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = ex.Message;
                Debug.WriteLine($"ReceiveOrder {numero} exception: {ex.Message}");
                return result;
            }
        }
    }
}
