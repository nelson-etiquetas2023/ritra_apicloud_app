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

        public async Task<ProcesarOrdenResult> ProcesarCompraAsync(string numero)
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

            bool hayFallo = false;

            foreach (var item in oc.Items)
            {
                var itemResult = await ProcesarItemCompraAsync(item);
                result.Items.Add(itemResult);
                if (!itemResult.Ok) hayFallo = true;
            }

            oc.Status = hayFallo ? 5 : 4;
            await context.SaveChangesAsync();

            result.Success = !hayFallo;
            result.Message = hayFallo
                ? $"El documento {numero} quedó en estado Transacción Fallida. Los productos con error no afectaron el inventario y el documento puede volver a procesarse."
                : $"El documento {numero} fue procesado exitosamente. Todos los productos afectaron el inventario.";
            result.StatusFinal = oc.Status;
            stopwatch.Stop();
            result.ElapsedMilliseconds = stopwatch.ElapsedMilliseconds;
            return result;
        }

        private async Task<ProcesarItemResult> ProcesarItemCompraAsync(DetalleCompras item)
        {
            var itemResult = new ProcesarItemResult
            {
                ProductCode = item.Product_id,
                ProductName = item.Product_name,
                Cantidad = item.Cantidad
            };

            if (item.Procesado)
            {
                itemResult.Ok = true;
                itemResult.YaProcesado = true;
                itemResult.Error = "La línea ya afectó el inventario en un procesamiento anterior.";
                return itemResult;
            }

            var producto = await ResolverProductoAsync(item.Product_id);
            if (producto is null)
            {
                itemResult.Ok = false;
                itemResult.Error = $"Producto '{item.Product_id}' no encontrado por código de barra.";
                return itemResult;
            }

            itemResult.StockAnterior = producto.Stock;
            producto.Stock += item.Cantidad;
            item.Procesado = true;
            item.FechaProcesado = DateTime.UtcNow;
            itemResult.StockNuevo = producto.Stock;
            itemResult.Ok = true;
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
                    TipoDocumento = string.IsNullOrWhiteSpace(linea.o.Tipo_Documento) ? "OC" : linea.o.Tipo_Documento,
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