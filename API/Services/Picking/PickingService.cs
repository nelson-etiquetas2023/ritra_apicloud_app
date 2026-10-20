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

            // Verificar que ninguno esté procesado (4) o anulado (6)
            var noTransferibles = pedidosExistentes
                .Where(p => p.Status == 4 || p.Status == 6)
                .Select(p => p.Numero)
                .ToList();

            if (noTransferibles.Count > 0)
            {
                result.Success = false;
                result.Message = $"Los siguientes pedidos no se pueden transferir (procesados o anulados): {string.Join(", ", noTransferibles)}";
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
            await context.SaveChangesAsync();

            result.Success = true;
            result.Message = $"{pickingOrders.Count} pedido(s) transferido(s) a picking correctamente.";
            result.Data = pickingOrders;
            return result;
        }

        public async Task<List<PedidoVenta>> GetPedidosParaPickingAsync()
        {
            // Obtener números de pedido que tienen picking pendiente (Estado 0 o 1)
            var numerosPicking = await context.PickingOrders
                .Where(po => po.Estado != 2)
                .Select(po => po.NumeroPedidoVenta)
                .ToListAsync();

            if (numerosPicking.Count == 0)
                return new List<PedidoVenta>();

            return await context.PedidoVenta
                .Include(p => p.Items)
                .Include(p => p.Pagos)
                .Where(p => numerosPicking.Contains(p.Numero))
                .OrderByDescending(p => p.Id)
                .ToListAsync();
        }

        public async Task<bool> CompletarPickingAsync(int pickingOrderId)
        {
            var picking = await context.PickingOrders
                .FirstOrDefaultAsync(po => po.Id == pickingOrderId);

            if (picking == null || picking.Estado == 2)
                return false;

            picking.Estado = 2;
            picking.FechaCompletado = DateTime.Now;
            await context.SaveChangesAsync();
            return true;
        }
    }
}
