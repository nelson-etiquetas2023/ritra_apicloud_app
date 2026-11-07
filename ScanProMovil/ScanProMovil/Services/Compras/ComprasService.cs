using Microsoft.EntityFrameworkCore;
using ScanProMovil.Data;
using ScanProMovil.Data.Entities;
using ScanProMovil.Data.Services;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;

// Alias: las clases del DTO compartido comparten nombre con las entidades locales.
using RecepcionCompraRequest = Shared.Dtos.Compras.RecepcionCompraRequest;
using LineaRecepcionCompra = Shared.Dtos.Compras.LineaRecepcionCompra;

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

        /// <summary>
        /// Baja TODAS las ordenes de compra del servidor (sin filtrar por estado:
        /// el sincronizador decide como mezclar 4/5/6 y propagar recepciones).
        /// </summary>
        public async Task<List<Shared.Dtos.Compras.OrdenCompra>> GetOrdenesRemotoAsync()
        {
            try
            {
                var client = httpFactory.CreateClient("scanpro");
                var response = await client.GetAsync("api/ordencompra/getorders");
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrWhiteSpace(json)) return new();
                var remotos = JsonSerializer.Deserialize<List<Shared.Dtos.Compras.OrdenCompra>>(json, jsonOptions) ?? new();
                return remotos;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error obteniendo compras remotas: {ex.Message}");
                return new();
            }
        }

        /// <summary>Lee los numeros de orden que el usuario borro localmente
        /// para no volver a bajarlos al sincronizar con el servidor.</summary>
        private List<string> GetNumerosOrdenesBorradas()
        {
            try
            {
                var json = Preferences.Get("OrdenesBorradasLocalmente", string.Empty);
                if (string.IsNullOrWhiteSpace(json)) return new List<string>();
                return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ComprasService] Error leyendo ordenes borradas: {ex.Message}");
                return new List<string>();
            }
        }

        /// <summary>Guarda los numeros de orden que el usuario borro localmente
        /// para no volver a bajarlos al sincronizar con el servidor.</summary>
        public void GuardarNumerosOrdenesBorradas(List<string> numeros)
        {
            try
            {
                var json = JsonSerializer.Serialize(numeros.Distinct().Where(n => !string.IsNullOrWhiteSpace(n)).ToList());
                Preferences.Set("OrdenesBorradasLocalmente", json);
                Debug.WriteLine($"[ComprasService] Guardadas {numeros.Count} orden(es) en lista negra de borrados.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ComprasService] Error guardando ordenes borradas: {ex.Message}");
            }
        }

        public async Task<bool> SincronizarComprasDesdeApiAsync()
        {
            try
            {
                var remotos = await GetOrdenesRemotoAsync();

                // FILTRAR: excluir las ordenes que el usuario borro localmente
                // para que no vuelvan a aparecer al sincronizar.
                var borradas = GetNumerosOrdenesBorradas();
                if (borradas.Count > 0)
                {
                    remotos = remotos.Where(r => !borradas.Contains(r.Numero)).ToList();
                    Debug.WriteLine($"[ComprasService] Filtradas {borradas.Count} ordenes borradas localmente al sincronizar.");
                }

                var datosRemotos = ComprasSincronizador.Mapear(remotos);
                if (datosRemotos.Count == 0) return true;

                await using var ctx = await _contextFactory.CreateDbContextAsync();
                var numerosRemotos = datosRemotos.Select(o => o.Numero).Distinct().ToList();

                var resultado = await ComprasSincronizador.AplicarAsync(ctx, datosRemotos, numerosRemotos);

                Debug.WriteLine($"[ComprasService] Sincronizador: insertadas={resultado.Insertadas}, actualizadas={resultado.Actualizadas}, pendientesDeRecepcion={resultado.PendientesDeRecepcionRemoto.Count}, cerradasLocalmente={resultado.CerradasLocalmente}");

                // Propagar al servidor lo ya recibido aqui (misma idea del picking:
                // lo que el dispositivo recepciono fuera de linea se envia apenas
                // hay datos, y reenviar lo mismo es un no-op en el servidor).
                foreach (var pendiente in resultado.PendientesDeRecepcionRemoto)
                {
                    var res = await ReceiveOrderAsync(pendiente.Numero);
                    Debug.WriteLine($"[ComprasService] Push recepcion {pendiente.Numero}: success={res.Success}, statusFinal={res.StatusFinal}, msg={res.Message}");
                }

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

        /// <summary>
        /// Genera el siguiente número de orden de compra con formato PO-9999.
        /// PO = Purchase Order, 9999 = consecutivo con 4 dígitos (relleno con ceros a la izquierda).
        /// Ejemplo: PO-0001, PO-0002, ..., PO-9999, PO-10000
        /// </summary>
        public async Task<string> GetNextOrderNumberAsync()
        {
            const string tipo = "PO"; // Purchase Order
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

        /// <summary>Un escaneo = una unidad recibida. Guarda en SQLite con
        /// tope en la cantidad de la linea (replica MarcarItemCompletadoAsync del picking).</summary>
        public async Task<bool> MarcarItemRecibidoAsync(string numeroOrden, int detailId)
        {
            try
            {
                await using var _context = await _contextFactory.CreateDbContextAsync();
                await using var tx = await _context.Database.BeginTransactionAsync();
                var item = await _context.PurchaseOrderItems
                    .FirstOrDefaultAsync(i => i.DetailId == detailId && i.Numero == numeroOrden);
                if (item == null) return false;

                // Nunca se pasa de la cantidad de la linea.
                if (item.CantidadRecibida < item.Cantidad)
                    item.CantidadRecibida++;

                await _context.SaveChangesAsync();
                await tx.CommitAsync();

                // Evidencia en log de que la linea quedo a disco (persistencia).
                var guardado = await _context.PurchaseOrderItems
                    .AsNoTracking()
                    .FirstOrDefaultAsync(i => i.DetailId == detailId && i.Numero == numeroOrden);
                Debug.WriteLine($"[ComprasService] Item {detailId} guardado en SQLite (orden {numeroOrden}): {guardado?.CantidadRecibida}/{guardado?.Cantidad} recibido.");

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ComprasService] Error marcando item {detailId}: {ex.Message}");
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

                // El cuerpo se construye desde la base local: declara la cantidad
                // TOTAL recibida hasta ahora por linea; el servidor calcula el
                // delta contra lo ya almacenado (reenviar lo mismo no vuelve a
                // afectar el inventario, como reenviar un picking completo).
                await using var ctx = await _contextFactory.CreateDbContextAsync();
                var ordenLocal = await ctx.PurchaseOrders
                    .AsNoTracking()
                    .Include(o => o.Items)
                    .FirstOrDefaultAsync(o => o.Numero == numero);

                if (ordenLocal is null)
                {
                    result.Message = $"La orden {numero} no existe en este dispositivo.";
                    return result;
                }

                var request = new RecepcionCompraRequest
                {
                    Usuario = ordenLocal.UserName,
                    Items = ordenLocal.Items.Select(i => new LineaRecepcionCompra
                    {
                        Product_id = i.Product_id,
                        CantidadRecibida = i.CantidadRecibida
                    }).ToList()
                };

                var clienteHttp = httpFactory.CreateClient("scanpro");
                var response = await clienteHttp.PostAsJsonAsync(
                    $"api/inventario/process-compra/{numero}", request, jsonOptions);

                var json = await response.Content.ReadAsStringAsync();
                if (!response.IsSuccessStatusCode)
                {
                    result.Message = $"HTTP {(int)response.StatusCode}: {json}";
                    Debug.WriteLine($"ReceiveOrder {numero} error: {result.Message}");
                    return result;
                }

                result = JsonSerializer.Deserialize<Shared.Dtos.Compras.ProcesarOrdenResult>(json, jsonOptions)
                         ?? new Shared.Dtos.Compras.ProcesarOrdenResult { Success = true, Message = "Procesado." };

                // Reflejar en la base local el cierre que el servidor ya aplico:
                // 4 Procesado (cerrada, nunca se sobrescribe) o 5 Transaccion
                // Fallida (sigue recepcionandose).
                if (result.StatusFinal is 4 or 5)
                {
                    await using var ctxUpdate = await _contextFactory.CreateDbContextAsync();
                    var orden = await ctxUpdate.PurchaseOrders
                        .FirstOrDefaultAsync(o => o.Numero == numero);

                    if (orden is not null && orden.Status != 4)
                    {
                        orden.Status = result.StatusFinal;
                        await ctxUpdate.SaveChangesAsync();
                    }
                }

                Debug.WriteLine($"ReceiveOrder {numero}: success={result.Success}, statusFinal={result.StatusFinal}, msg={result.Message}");
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

        /// <summary>Borra TODAS las órdenes de compra locales de SQLite (irreversible).</summary>
        public async Task<bool> DeleteAllOrdersLocalAsync()
        {
            try
            {
                await using var _context = await _contextFactory.CreateDbContextAsync();

                // Borrar detalles de compra primero (por clave foránea cascade)
                var detallesBorrados = await _context.PurchaseOrderItems
                    .ExecuteDeleteAsync();

                // Borrar todas las órdenes de compra locales
                var ordenesBorradas = await _context.PurchaseOrders
                    .ExecuteDeleteAsync();

                Debug.WriteLine($"[ComprasService] Borradas {ordenesBorradas} orden(es) y {detallesBorrados} detalle(s) de SQLite.");

                return ordenesBorradas >= 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ComprasService] Error borrando órdenes locales: {ex.Message}");
                return false;
            }
        }

        /// <summary>Borra TODAS las órdenes de compra del servidor (irreversible).</summary>
        public async Task<bool> DeleteAllOrdersRemotoAsync()
        {
            try
            {
                var clienteHttp = httpFactory.CreateClient("scanpro");
                var response = await clienteHttp.DeleteAsync("api/ordencompra/deleteall");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error borrando todas las órdenes del servidor: {ex.Message}");
                return false;
            }
        }
    }
}
