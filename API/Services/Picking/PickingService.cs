using API.Data;
using Microsoft.EntityFrameworkCore;
using Shared.Dtos.Picking;
using Shared.Dtos.Ventas;

namespace API.Services.Picking
{
    public class PickingService(ApplicationDbContext context) : IPickingService
    {
        public async Task<PickingOrderResult> TransferirAPickingAsync(List<string> numerosPedido, string? usuario)
        {
            var result = new PickingOrderResult();

            if (numerosPedido == null || numerosPedido.Count == 0)
            {
                result.Success = false;
                result.Message = "No se seleccionaron pedidos para transferir a picking.";
                return result;
            }

            // Eliminar duplicados y vacíos
            var numeros = numerosPedido
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Select(n => n.Trim())
                .Distinct()
                .ToList();

            if (numeros.Count == 0)
            {
                result.Success = false;
                result.Message = "No se seleccionaron pedidos válidos.";
                return result;
            }

            // Verificar que los pedidos existen y no están ya en picking, procesados o anulados
            var pedidosExistentes = await context.PedidoVenta
                .Where(p => numeros.Contains(p.Numero))
                .ToListAsync();

            if (pedidosExistentes.Count != numeros.Count)
            {
                var faltantes = numeros.Except(pedidosExistentes.Select(p => p.Numero)).ToList();
                result.Success = false;
                result.Message = $"Los siguientes pedidos no existen: {string.Join(", ", faltantes)}";
                return result;
            }

            // Verificar que ninguno esté ya en picking
            var yaEnPicking = await context.PickingOrders
                .Where(po => numeros.Contains(po.NumeroPedidoVenta) && po.Estado != 2)
                .Select(po => po.NumeroPedidoVenta)
                .ToListAsync();

            if (yaEnPicking.Count > 0)
            {
                result.Success = false;
                result.Message = $"Los siguientes pedidos ya están en picking: {string.Join(", ", yaEnPicking)}";
                return result;
            }

            // Verificar que ninguno esté procesado (4), anulado (6) o ya en picking (7)
            var noTransferibles = pedidosExistentes
                .Where(p => p.Status == PedidoVentaStatus.Procesado ||
                            p.Status == PedidoVentaStatus.Anulado ||
                            p.Status == PedidoVentaStatus.Picking)
                .Select(p => p.Numero)
                .ToList();

            if (noTransferibles.Count > 0)
            {
                result.Success = false;
                result.Message = $"Los siguientes pedidos no se pueden transferir (procesados, anulados o ya en picking): {string.Join(", ", noTransferibles)}";
                return result;
            }

            // Crear registros de picking
            var ahora = DateTime.Now;
            var pickingOrders = new List<PickingOrder>();

            foreach (var numero in numeros)
            {
                pickingOrders.Add(new PickingOrder
                {
                    NumeroPedidoVenta = numero,
                    Estado = 0, // Pendiente
                    UsuarioAsignado = usuario,
                    FechaCreacion = ahora
                });
            }

            context.PickingOrders.AddRange(pickingOrders);

            // Los pedidos quedan marcados con estatus Picking para que la web
            // los muestre como "en picking" y el device los traiga al sincronizar.
            foreach (var pedido in pedidosExistentes)
            {
                pedido.Status = PedidoVentaStatus.Picking;
            }

            await context.SaveChangesAsync();

            result.Success = true;
            result.Message = $"{pickingOrders.Count} pedido(s) transferido(s) a picking correctamente.";
            result.Data = pickingOrders;
            return result;
        }

        public async Task<List<PickingPendienteDto>> GetPedidosParaPickingAsync()
        {
            // Ordenes de picking que siguen abiertas (Estado 0 o 1)
            var picking = await context.PickingOrders
                .Where(po => po.Estado != 2)
                .OrderBy(po => po.Id)
                .ToListAsync();

            if (picking.Count == 0)
                return [];

            var numeros = picking.Select(po => po.NumeroPedidoVenta).ToList();

            var pedidos = await context.PedidoVenta
                .Include(p => p.Items)
                .Include(p => p.Pagos)
                .Where(p => numeros.Contains(p.Numero))
                .ToDictionaryAsync(p => p.Numero);

            // El dispositivo necesita el Id de la orden de picking para poder
            // completarla en el servidor, por eso se devuelve junto al pedido.
            return picking
                .Where(po => pedidos.ContainsKey(po.NumeroPedidoVenta))
                .Select(po => new PickingPendienteDto
                {
                    PickingId = po.Id,
                    NumeroPedidoVenta = po.NumeroPedidoVenta,
                    Estado = po.Estado,
                    UsuarioAsignado = po.UsuarioAsignado,
                    FechaCreacion = po.FechaCreacion,
                    FechaCompletado = po.FechaCompletado,
                    Pedido = pedidos[po.NumeroPedidoVenta]
                })
                .ToList();
        }

        public async Task<bool> CompletarPickingAsync(int pickingOrderId)
        {
            var picking = await context.PickingOrders
                .FirstOrDefaultAsync(po => po.Id == pickingOrderId);

            if (picking == null || picking.Estado == 2)
                return false;

            picking.Estado = 2;
            picking.FechaCompletado = DateTime.Now;

            // El pedido vuelve a quedar disponible para procesar en la web.
            var pedido = await context.PedidoVenta
                .FirstOrDefaultAsync(p => p.Numero == picking.NumeroPedidoVenta);

            if (pedido != null && pedido.Status == PedidoVentaStatus.Picking)
            {
                pedido.Status = PedidoVentaStatus.Planificado;
            }

            await context.SaveChangesAsync();
            return true;
        }

        public async Task<PickingOrderResult> CancelarPickingAsync(string? numeroPedido)
        {
            var result = new PickingOrderResult();

            if (string.IsNullOrWhiteSpace(numeroPedido))
            {
                result.Success = false;
                result.Message = "No se indicó el pedido a sacar de picking.";
                return result;
            }

            var numero = numeroPedido.Trim();

            var picking = await context.PickingOrders
                .Where(po => po.NumeroPedidoVenta == numero && po.Estado != 2)
                .OrderByDescending(po => po.Id)
                .FirstOrDefaultAsync();

            if (picking == null)
            {
                result.Success = false;
                result.Message = $"El pedido {numero} no está en picking.";
                return result;
            }

            context.PickingOrders.Remove(picking);

            var pedido = await context.PedidoVenta.FirstOrDefaultAsync(p => p.Numero == numero);
            if (pedido != null && pedido.Status == PedidoVentaStatus.Picking)
            {
                pedido.Status = PedidoVentaStatus.Planificado;
            }

            await context.SaveChangesAsync();

            result.Success = true;
            result.Message = $"El pedido {numero} salió de picking.";
            return result;
        }
    }
}
