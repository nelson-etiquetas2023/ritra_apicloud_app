using API.Data;
using Microsoft.EntityFrameworkCore;
using Shared.Dtos.Compras;

namespace API.Services.OcMovil
{
    public class OcMovilService(ApplicationDbContext context) : IOcMovilService
    {
        private readonly ApplicationDbContext context = context;

        public async Task<IEnumerable<OrdenCompra>> GetOrdersAsync()
        {
            var orders = await context.Compra
                .Include(o => o.Items)
                .OrderByDescending(o => o.FechaCreacion)
                .ThenByDescending(o => o.Numero)
                .ToListAsync();
            return orders;
        }

        public async Task<OrdenCompra> GetOrderByIdAsync(string numero)
        {
            var oc = await context.Compra.Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Numero == numero);

            return oc is null ? throw new KeyNotFoundException($"Orden {numero} no encontrada") : oc;
        }

        public async Task<string> GetNextNumAsync()
        {
            var max = await context.Database.SqlQuery<int?>($"""
                SELECT MAX(CAST(SUBSTRING(Numero, 4, 10) AS INT)) AS Value
                FROM Compra
                WHERE Numero LIKE 'OC-%' AND SUBSTRING(Numero, 4, 10) NOT LIKE '%[^0-9]%'
                """).FirstOrDefaultAsync() ?? 0;

            return $"OC-{max + 1:0000}";
        }

        public async Task<OrdenCompra?> AddOrderAsync(OrdenCompra oc)
        {
            if (oc is null) return null;

            if (oc.FechaCreacion == default)
                oc.FechaCreacion = DateTime.Now;

            if (string.IsNullOrWhiteSpace(oc.Numero))
            {
                var siguiente = int.Parse((await GetNextNumAsync()).Substring(3));
                while (await context.Compra.AnyAsync(o => o.Numero == $"OC-{siguiente:0000}"))
                    siguiente++;
                oc.Numero = $"OC-{siguiente:0000}";
            }
            else if (await context.Compra.AnyAsync(o => o.Numero == oc.Numero))
            {
                return null;
            }

            oc.Prioridad = ValidarPrioridad(oc.Prioridad);
            oc.Status = 0;

            foreach (var item in oc.Items)
            {
                item.Id = 0;
                item.Comentario = item.Comentario?.Trim() ?? string.Empty;
            }

            context.Compra.Add(oc);
            await context.SaveChangesAsync();
            return oc;
        }

        public async Task<OrdenCompra?> UpdateOrderAsync(string Numero, OrdenCompra oc)
        {
            var existingOrder = await context.Compra.Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Numero == Numero);

            if (existingOrder is null) return null;

            //Documento procesado o anulado: no se permite modificar.
            if (existingOrder.Status == 4 || existingOrder.Status == 6) return null;

            existingOrder.Description = oc.Description;
            existingOrder.Comentario = oc.Comentario;
            existingOrder.Fecha = oc.Fecha;
            if (oc.FechaCreacion != default)
                existingOrder.FechaCreacion = oc.FechaCreacion;
            existingOrder.Supply_Id = oc.Supply_Id;
            existingOrder.Supply_Name = oc.Supply_Name;
            existingOrder.Reference = oc.Reference;
            existingOrder.WarehouseName = oc.WarehouseName;
            existingOrder.Prioridad = ValidarPrioridad(oc.Prioridad);
            existingOrder.Status = 1;
            existingOrder.Subtotal = oc.Subtotal;
            existingOrder.Impuesto = oc.Impuesto;
            existingOrder.Total = oc.Total;
            existingOrder.Sincro = oc.Sincro;

            await using var transaction = await context.Database.BeginTransactionAsync();
            try
            {
                context.DetalleCompra.RemoveRange(existingOrder.Items);

                var newItems = new List<DetalleCompras>();

                foreach (var item in oc.Items)
                {
                    var newItem = new DetalleCompras
                    {
                        Numero = Numero,

                        Product_id = item.Product_id,
                        Product_name = item.Product_name,
                        Cantidad = item.Cantidad,
                        Costo = item.Costo,
                        Subtotal = item.Subtotal,
                        Comentario = item.Comentario?.Trim() ?? string.Empty,
                    };
                    newItems.Add(newItem);
                }
                await context.DetalleCompra.AddRangeAsync(newItems);
                await context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
            return existingOrder;
        }

        public async Task<bool> AnularOrderAsync(string numero)
        {
            var oc = await context.Compra.FirstOrDefaultAsync(o => o.Numero == numero);
            if (oc == null || oc.Status == 4 || oc.Status == 6) return false;

            oc.Status = 6;
            await context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteOrderAsync(string numero)
        {
            var OrderDeleted = await context.Compra.FindAsync(numero);

            if (OrderDeleted is null) return false;

            //Documento procesado o anulado: no se permite eliminar.
            if (OrderDeleted.Status == 4 || OrderDeleted.Status == 6) return false;

            context.Compra.Remove(OrderDeleted);
            await context.SaveChangesAsync();
            return true;
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
    }
}
