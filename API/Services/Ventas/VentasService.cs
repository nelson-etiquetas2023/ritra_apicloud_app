using API.Data;
using Microsoft.EntityFrameworkCore;
using Shared.Dtos;
using Shared.Dtos.CargasIniciales;
using Shared.Dtos.Ventas;

namespace API.Services.Ventas
{
    public class VentasService(ApplicationDbContext context) : IVentasService
    {
        public async Task<List<PedidoVenta>> GetAllAsync()
        {
            return await context.PedidoVenta
                .Include(p => p.Items)
                .OrderByDescending(p => p.Id)
                .ToListAsync();
        }

        public async Task<PedidoVenta?> GetByIdAsync(int id)
        {
            return await context.PedidoVenta
                .Include(p => p.Items)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<PedidoVentaSaveResult> CreateAsync(PedidoVenta pedido)
        {
            var result = new PedidoVentaSaveResult();

            if (pedido == null)
            {
                result.Success = false;
                result.Message = "Datos inválidos.";
                return result;
            }

            if (string.IsNullOrWhiteSpace(pedido.Numero))
            {
                result.Success = false;
                result.Message = "El número del pedido es obligatorio.";
                return result;
            }

            if (pedido.Cliente_Id <= 0)
            {
                result.Success = false;
                result.Message = "Debe seleccionar un cliente.";
                return result;
            }

            if (pedido.Items == null || pedido.Items.Count == 0)
            {
                result.Success = false;
                result.Message = "El pedido debe tener al menos una línea de detalle.";
                return result;
            }

            var cliente = await context.Customers.FirstOrDefaultAsync(c => c.customer_id == pedido.Cliente_Id);
            if (cliente == null)
            {
                result.Success = false;
                result.Message = "El cliente seleccionado no existe.";
                return result;
            }

            var yaExiste = await context.PedidoVenta.AnyAsync(p => p.Numero == pedido.Numero);
            if (yaExiste)
            {
                result.Success = false;
                result.Message = $"El pedido {pedido.Numero} ya existe.";
                return result;
            }

            var errores = ValidarDetalles(pedido);
            if (errores.Count > 0)
            {
                result.Success = false;
                result.Message = "El pedido no se guardó. Corrige los errores del detalle.";
                result.Errors = errores;
                return result;
            }

            pedido.Cliente_Nombre = cliente.CustomerName;
            pedido.Cliente_RNC = cliente.RNC ?? "";
            pedido.Prioridad = ValidarPrioridad(pedido.Prioridad);
            pedido.Status = 0;
            pedido.Id = 0;
            foreach (var detalle in pedido.Items)
            {
                detalle.Id = 0;
                detalle.PedidoVentaId = 0;
            }

            RecalcularTotales(pedido);

            context.PedidoVenta.Add(pedido);
            await context.SaveChangesAsync();

            result.Success = true;
            result.Message = $"El pedido {pedido.Numero} fue guardado correctamente.";
            result.Data = await GetByIdAsync(pedido.Id);
            return result;
        }

        public async Task<PedidoVentaSaveResult> UpdateAsync(int id, PedidoVenta pedido)
        {
            var result = new PedidoVentaSaveResult();

            var existing = await context.PedidoVenta
                .Include(p => p.Items)
                .FirstOrDefaultAsync(p => p.Id == id);
            if (existing == null)
            {
                result.Success = false;
                result.Message = $"El pedido de venta {id} no fue encontrado.";
                return result;
            }

            if (existing.Status == 4)
            {
                result.Success = false;
                result.Message = "El documento ya fue procesado y no puede editarse.";
                return result;
            }

            if (existing.Status == 6)
            {
                result.Success = false;
                result.Message = "El documento está anulado y no puede editarse.";
                return result;
            }

            if (pedido.Cliente_Id <= 0)
            {
                result.Success = false;
                result.Message = "Debe seleccionar un cliente.";
                return result;
            }

            if (pedido.Items == null || pedido.Items.Count == 0)
            {
                result.Success = false;
                result.Message = "El pedido debe tener al menos una línea de detalle.";
                return result;
            }

            var cliente = await context.Customers.FirstOrDefaultAsync(c => c.customer_id == pedido.Cliente_Id);
            if (cliente == null)
            {
                result.Success = false;
                result.Message = "El cliente seleccionado no existe.";
                return result;
            }

            var errores = ValidarDetalles(pedido);
            if (errores.Count > 0)
            {
                result.Success = false;
                result.Message = "El pedido no se actualizó. Corrige los errores del detalle.";
                result.Errors = errores;
                return result;
            }

            existing.Fecha = pedido.Fecha;
            existing.FechaCreacion = pedido.FechaCreacion;
            existing.Cliente_Id = pedido.Cliente_Id;
            existing.Cliente_Nombre = cliente.CustomerName;
            existing.Cliente_RNC = cliente.RNC ?? "";
            existing.DireccionEntrega = pedido.DireccionEntrega;
            existing.Vendedor = pedido.Vendedor;
            existing.Prioridad = ValidarPrioridad(pedido.Prioridad);
            existing.WarehouseName = pedido.WarehouseName;
            existing.Reference = pedido.Reference;
            existing.Description = pedido.Description;
            existing.Status = 1;

            context.PedidoVentaDetalles.RemoveRange(existing.Items);
            await context.SaveChangesAsync();

            foreach (var detalle in pedido.Items)
            {
                detalle.Id = 0;
                detalle.PedidoVentaId = id;
                context.PedidoVentaDetalles.Add(detalle);
            }

            RecalcularTotales(pedido);
            existing.Subtotal = pedido.Subtotal;
            existing.Descuento = pedido.Descuento;
            existing.Impuesto = pedido.Impuesto;
            existing.Total = pedido.Total;

            await context.SaveChangesAsync();

            result.Success = true;
            result.Message = $"El pedido {existing.Numero} fue actualizado correctamente.";
            result.Data = await GetByIdAsync(id);
            return result;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var pedido = await context.PedidoVenta.FirstOrDefaultAsync(p => p.Id == id);
            if (pedido == null) return false;

            context.PedidoVenta.Remove(pedido);
            await context.SaveChangesAsync();
            return true;
        }

        public async Task<string> GetNextNumAsync()
        {
            var max = await context.Database.SqlQuery<int?>($"""
                SELECT MAX(CAST(SUBSTRING(Numero, 4, 10) AS INT)) AS Value
                FROM PedidoVenta
                WHERE Numero LIKE 'PV-%' AND SUBSTRING(Numero, 4, 10) NOT LIKE '%[^0-9]%'
                """).FirstOrDefaultAsync() ?? 0;

            return $"PV-{max + 1:0000}";
        }

        private static List<RowError> ValidarDetalles(PedidoVenta pedido)
        {
            var errores = new List<RowError>();
            foreach (var detalle in pedido.Items)
            {
                if (string.IsNullOrWhiteSpace(detalle.Product_id))
                {
                    errores.Add(new RowError { Row = 0, Message = "El código de producto es obligatorio en cada línea." });
                    continue;
                }

                if (detalle.Cantidad <= 0)
                {
                    errores.Add(new RowError { Row = 0, Message = $"La cantidad del producto '{detalle.Product_id}' debe ser mayor a cero." });
                }
            }
            return errores;
        }

        private static string ValidarPrioridad(string? prioridad)
        {
            return prioridad?.Trim() switch
            {
                "Media" => "Media",
                "Alta" => "Alta",
                "Urgente" => "Urgente",
                _ => "Normal"
            };
        }

        private static void RecalcularTotales(PedidoVenta pv)
        {
            pv.Subtotal = pv.Items.Sum(i => i.Cantidad * i.Precio);
            pv.Descuento = pv.Items.Sum(i => i.Cantidad * i.Precio * (i.Descuento / 100m));
            var neto = pv.Subtotal - pv.Descuento;
            pv.Impuesto = neto * 0.18m;
            pv.Total = neto + pv.Impuesto;
        }

        #region PROCESAR-PEDIDO

        public async Task<bool> AnularAsync(int id)
        {
            var pedido = await context.PedidoVenta.FirstOrDefaultAsync(p => p.Id == id);
            if (pedido == null || pedido.Status == 4 || pedido.Status == 6) return false;

            pedido.Status = 6;
            await context.SaveChangesAsync();
            return true;
        }

        public async Task<ProcesarPedidoResult> ProcesarPedidoAsync(string numero)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var result = new ProcesarPedidoResult();

            var pedido = await context.PedidoVenta
                .Include(p => p.Items)
                .FirstOrDefaultAsync(p => p.Numero == numero);

            if (pedido is null)
            {
                stopwatch.Stop();
                result.Success = false;
                result.Message = $"El documento {numero} no fue encontrado.";
                result.ElapsedMilliseconds = stopwatch.ElapsedMilliseconds;
                return result;
            }

            if (pedido.Status == 4)
            {
                stopwatch.Stop();
                result.Success = false;
                result.Message = $"El documento {numero} ya fue procesado y no puede volver a procesarse.";
                result.ElapsedMilliseconds = stopwatch.ElapsedMilliseconds;
                result.StatusFinal = pedido.Status;
                return result;
            }

            if (pedido.Status == 6)
            {
                stopwatch.Stop();
                result.Success = false;
                result.Message = $"El documento {numero} está anulado y no puede procesarse.";
                result.ElapsedMilliseconds = stopwatch.ElapsedMilliseconds;
                result.StatusFinal = pedido.Status;
                return result;
            }

            bool hayFallo = false;

            foreach (var item in pedido.Items)
            {
                var itemResult = await ProcesarItemVentaAsync(item);
                result.Items.Add(itemResult);
                if (!itemResult.Ok) hayFallo = true;
            }

            pedido.Status = hayFallo ? 5 : 4;
            await context.SaveChangesAsync();

            result.Success = !hayFallo;
            result.Message = hayFallo
                ? $"El documento {numero} quedó en estado Transacción Fallida. Los productos con error no afectaron el inventario y el documento puede volver a procesarse."
                : $"El documento {numero} fue procesado exitosamente. Todos los productos descontaron inventario.";
            result.StatusFinal = pedido.Status;
            stopwatch.Stop();
            result.ElapsedMilliseconds = stopwatch.ElapsedMilliseconds;
            return result;
        }

        private async Task<ProcesarItemVentaResult> ProcesarItemVentaAsync(DetalleVenta item)
        {
            var itemResult = new ProcesarItemVentaResult
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

            //Descarga atomica con guarda: si el stock no alcanza, rows == 0 y la linea falla sin tocar inventario.
            var rows = await context.Productos
                .Where(p => p.Product_id == producto.Product_id && p.Stock - item.Cantidad >= 0)
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.Stock, p => p.Stock - (double)item.Cantidad));

            if (rows == 0)
            {
                itemResult.Ok = false;
                itemResult.Error = $"Stock insuficiente para '{producto.Product_Name}'. Disponible: {producto.Stock:N2}, solicitado: {item.Cantidad}.";
                return itemResult;
            }

            itemResult.StockNuevo = producto.Stock - item.Cantidad;
            item.Procesado = true;
            item.FechaProcesado = DateTime.Now;
            itemResult.Ok = true;

            //Cada linea confirma su propia marca de aplicado (transaccion por producto, idempotencia).
            await context.SaveChangesAsync();
            return itemResult;
        }

        private async Task<Product?> ResolverProductoAsync(string codigo)
        {
            if (string.IsNullOrWhiteSpace(codigo)) return null;

            var normalized = codigo.Trim();
            return await context.Productos.FirstOrDefaultAsync(p =>
                (p.Codebar != null && p.Codebar.ToLower() == normalized.ToLower()) ||
                (p.Product_Code != null && p.Product_Code.ToLower() == normalized.ToLower()));
        }

        #endregion
    }
}
