using API.Data;
using Microsoft.EntityFrameworkCore;
using Shared.Dtos;
using Shared.Dtos.Compras;
using Shared.Dtos.Inventario;
using System.Diagnostics;

namespace API.Services.Inventario
{
    public class InventarioService(ApplicationDbContext context) : IInventarioService
    {
        private readonly ApplicationDbContext context = context;

        #region RECEIVE-COMPRAS

        /// <summary>
        /// Declaracion de recepcion de una orden de compra (vias de escaneo del
        /// movil, replicando el cierre del picking). El cuerpo trae la cantidad
        /// TOTAL recibida hasta ahora por linea; el servidor calcula el delta
        /// contra lo ya almacenado, de modo que reenviar lo mismo nunca vuelve a
        /// afectar el inventario. Solo las lineas completas (recibida >= ordenada)
        /// se marcan Procesado; el documento queda Procesado (4) cuando todas las
        /// lineas estan completas, en Transaccion Fallida (5) si alguna linea
        /// fallo, y sigue Por Recibir (2) en recepciones parciales exitosas.
        /// </summary>
        public async Task<ProcesarOrdenResult> ProcesarCompraAsync(string numero, RecepcionCompraRequest? recepcion)
        {
            var stopwatch = Stopwatch.StartNew();
            var result = new ProcesarOrdenResult();

            var oc = await context.Compra
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Numero == numero);

            if (oc is null)
            {
                stopwatch.Stop();
                result.Success = false;
                result.Message = $"El documento {numero} no fue encontrado.";
                result.ElapsedMilliseconds = stopwatch.ElapsedMilliseconds;
                return result;
            }

            if (oc.Status == 4)
            {
                stopwatch.Stop();
                result.Success = false;
                result.Message = $"El documento {numero} ya fue procesado y no puede volver a procesarse.";
                result.ElapsedMilliseconds = stopwatch.ElapsedMilliseconds;
                result.StatusFinal = oc.Status;
                return result;
            }

            if (oc.Status == 6)
            {
                stopwatch.Stop();
                result.Success = false;
                result.Message = $"El documento {numero} está anulado y no puede procesarse.";
                result.ElapsedMilliseconds = stopwatch.ElapsedMilliseconds;
                result.StatusFinal = oc.Status;
                return result;
            }

            var lineas = (recepcion?.Items ?? [])
                .Where(l => l is not null && !string.IsNullOrWhiteSpace(l.Product_id))
                .GroupBy(l => l.Product_id.Trim())
                .ToDictionary(
                    g => g.Key,
                    g => g.OrderByDescending(l => l.CantidadRecibida).First(),
                    StringComparer.OrdinalIgnoreCase);

            if (lineas.Count == 0)
            {
                stopwatch.Stop();
                result.Success = false;
                result.Message = "La recepción no trae líneas: envíe la cantidad recibida de cada producto.";
                result.ElapsedMilliseconds = stopwatch.ElapsedMilliseconds;
                return result;
            }

            bool hayFallo = false;

            foreach (var item in oc.Items)
            {
                var llave = item.Product_id?.Trim() ?? string.Empty;
                if (!lineas.TryGetValue(llave, out var linea))
                    continue; // lineas sin datos del dispositivo no se tocan.

                result.Items.Add(await ProcesarItemCompraAsync(item, linea));
                if (!result.Items[^1].Ok) hayFallo = true;
            }

            // Lineas de la recepcion que no existen en la orden: reportadas como fallo.
            var conocidos = oc.Items.Select(i => i.Product_id?.Trim() ?? string.Empty).ToHashSet(StringComparer.OrdinalIgnoreCase);
            foreach (var linea in lineas.Values)
            {
                if (conocidos.Contains(linea.Product_id.Trim())) continue;
                result.Items.Add(new ProcesarItemResult
                {
                    ProductCode = linea.Product_id.Trim(),
                    Ok = false,
                    Error = $"El producto '{linea.Product_id}' no pertenece a la orden {numero}."
                });
                hayFallo = true;
            }

            var ordenCompleta = oc.Items.All(i => i.Cantidad <= 0 || i.CantidadRecibida >= i.Cantidad);
            oc.Status = ordenCompleta ? 4 : hayFallo ? 5 : 2;
            await context.SaveChangesAsync();

            result.Success = !hayFallo;
            result.Message = ordenCompleta
                ? $"El documento {numero} fue recibido y procesado exitosamente. Todos los productos afectaron el inventario."
                : hayFallo
                    ? $"El documento {numero} quedó en estado Transacción Fallida. Las líneas con error no afectaron el inventario y el documento puede seguir recibiéndose."
                    : $"Recepción parcial ({oc.Items.Sum(i => i.CantidadRecibida)}/{oc.Items.Sum(i => i.Cantidad)}) registrada para el documento {numero}. El documento sigue Por Recibir.";
            result.StatusFinal = oc.Status;
            stopwatch.Stop();
            result.ElapsedMilliseconds = stopwatch.ElapsedMilliseconds;
            return result;
        }

        private async Task<ProcesarItemResult> ProcesarItemCompraAsync(DetalleCompras item, LineaRecepcionCompra linea)
        {
            var itemResult = new ProcesarItemResult
            {
                ProductCode = item.Product_id,
                ProductName = item.Product_name,
                Cantidad = item.Cantidad
            };

            // Compatibilidad con lineas procesadas por el flujo antiguo (sin
            // tracking de recepcion): quedaron Procesado=true con CantidadRecibida
            // en 0, pero el flujo viejo sumo a inventario la cantidad ordenada
            // completa. Se asimilan como recibidas completas para no duplicar
            // stock al recibirlas otra vez.
            if (item.Procesado && item.CantidadRecibida == 0 && item.Cantidad > 0)
                item.CantidadRecibida = item.Cantidad;

            // La cantidad recibida nunca baja ni excede lo ordenado: el estado
            // de otros dispositivos no se retrocede por uno viejo del actual.
            var recibeAhora = (int)Math.Clamp(Math.Floor(linea.CantidadRecibida), item.CantidadRecibida, item.Cantidad);
            var delta = recibeAhora - item.CantidadRecibida;
            var completa = item.Cantidad <= 0 || recibeAhora >= item.Cantidad;

            itemResult.CantidadAnteriorRecibida = item.CantidadRecibida;
            itemResult.CantidadRecibida = recibeAhora;
            itemResult.Delta = delta;

            if (item.Procesado && completa && delta <= 0)
            {
                itemResult.Ok = true;
                itemResult.YaProcesado = true;
                itemResult.Error = "La línea ya está recibida completa y afectó el inventario.";
                return itemResult;
            }

            if (delta > 0)
            {
                var producto = await ResolverProductoAsync(item.Product_id);
                if (producto is null)
                {
                    itemResult.Ok = false;
                    itemResult.Error = $"Producto '{item.Product_id}' no encontrado por código de barra.";
                    return itemResult;
                }

                itemResult.StockAnterior = producto.Stock;
                producto.Stock += delta;
                itemResult.StockNuevo = producto.Stock;
            }

            itemResult.Ok = true;
            item.CantidadRecibida = recibeAhora;
            item.FechaRecepcion = DateTime.UtcNow;

            if (completa)
            {
                // Linea cerrada: se marca como procesada la primera vez que
                // llega completa.
                if (!item.Procesado)
                {
                    item.Procesado = true;
                    item.FechaProcesado = DateTime.UtcNow;
                }
            }

            return itemResult;
        }

        private async Task<Product?> ResolverProductoAsync(string codigo)
        {
            if (string.IsNullOrWhiteSpace(codigo)) return null;

            var normalized = codigo.Trim();
            return await context.Productos.FirstOrDefaultAsync(p =>
                (p.Codebar != null && p.Codebar == normalized) ||
                (p.Product_Code != null && p.Product_Code == normalized));
        }

        #endregion

        #region PICKING

        #endregion

        #region PACKING

        #endregion

        #region TRANSFERENCIAS

        #endregion

        #region CONTEO-INVENTARIO

        public async Task<MovimientosProductoResult> GetMovimientosProductoAsync(string codigo)
        {
            var result = new MovimientosProductoResult();

            if (string.IsNullOrWhiteSpace(codigo)) return result;

            var normalized = codigo.Trim();
            var producto = await context.Productos.FirstOrDefaultAsync(p =>
                (p.Codebar != null && p.Codebar.ToLower() == normalized.ToLower()) ||
                (p.Product_Code != null && p.Product_Code.ToLower() == normalized.ToLower()));

            result.ProductId = producto?.Product_id ?? 0;
            result.ProductCode = codigo;
            result.ProductName = producto?.Product_Name ?? "";

            var codigoA = producto?.Codebar?.Trim() ?? "";
            var codigoB = producto?.Product_Code?.Trim() ?? "";

            var lineas = await context.DetalleCompra
                .Where(d => d.Procesado &&
                    (d.Product_id.Trim() == normalized ||
                     (!string.IsNullOrWhiteSpace(codigoA) && d.Product_id.Trim() == codigoA) ||
                     (!string.IsNullOrWhiteSpace(codigoB) && d.Product_id.Trim() == codigoB)))
                .Join(context.Compra,
                      d => d.Numero,
                      o => o.Numero,
                      (d, o) => new { d, o })
                .OrderBy(x => x.o.FechaCreacion)
                .ThenBy(x => x.o.Numero)
                .ToListAsync();

            double stockActual = 0;
            int totalCantidad = 0;
            decimal totalSubtotal = 0;

            foreach (var linea in lineas)
            {
                var anterior = stockActual;
                stockActual += linea.d.Cantidad;
                totalCantidad += linea.d.Cantidad;
                totalSubtotal += linea.d.Subtotal;

                result.Movimientos.Add(new MovimientoInventario
                {
                    Numero = linea.o.Numero,
                    TipoDocumento = string.IsNullOrWhiteSpace(linea.o.Tipo_Documento) ? "PO" : linea.o.Tipo_Documento,
                    Fecha = linea.o.Fecha,
                    FechaProcesado = linea.d.FechaProcesado,
                    Proveedor = linea.o.Supply_Name,
                    TipoMovimiento = "Entrada",
                    ProductCode = linea.d.Product_id,
                    ProductName = linea.d.Product_name,
                    Cantidad = linea.d.Cantidad,
                    Costo = linea.d.Costo,
                    Subtotal = linea.d.Subtotal,
                    StockAnterior = anterior,
                    StockNuevo = stockActual,
                    Usuario = linea.o.UserName
                });
            }

            result.TotalCantidad = totalCantidad;
            result.TotalSubtotal = totalSubtotal;
            result.StockActual = producto?.Stock ?? stockActual;

            // Ajustes (carga inicial y futuros conteos): quedan en el historial del botón de movimientos.
            if (producto != null)
            {
                var ajustes = await context.AjustesInventario
                    .Where(a => a.ProductoId == producto.Product_id)
                    .OrderBy(a => a.Fecha)
                    .ToListAsync();
                foreach (var a in ajustes)
                {
                    result.Movimientos.Add(new MovimientoInventario
                    {
                        Numero = a.Lote,
                        TipoDocumento = a.Motivo,
                        Fecha = a.Fecha,
                        FechaProcesado = a.Fecha,
                        TipoMovimiento = "Ajuste",
                        ProductCode = a.Codigo,
                        ProductName = producto.Product_Name ?? "",
                        Cantidad = (int)(a.StockNuevo - a.StockAnterior),
                        StockAnterior = a.StockAnterior,
                        StockNuevo = a.StockNuevo,
                        Usuario = a.Usuario
                    });
                }
                result.Movimientos = result.Movimientos.OrderBy(m => m.Fecha).ToList();
            }
            return result;
        }

        #endregion

        #region FACTURACION

        #endregion
    }
}