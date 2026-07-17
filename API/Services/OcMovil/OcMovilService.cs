using API.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared.Dtos.Compras;

namespace API.Services.OcMovil
{
    public class OcMovilService(ApplicationDbContext context) : IOcMovilService
    {
        private readonly ApplicationDbContext context = context;

        public async Task<IEnumerable<OrdenCompra>> GetOrdersAsync()
        {
            var orders = await context.Compra.Include(o => o.Items).ToListAsync();
            return orders;
        }

        public async Task<OrdenCompra> GetOrderByIdAsync(string numero)
        {
            var oc = await context.Compra.Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Numero == numero);

            return oc is null ? throw new KeyNotFoundException($"Orden {numero} no encontrada") : oc;
        }

        public async Task<OrdenCompra?> AddOrderAsync(OrdenCompra oc)
        {
            if (oc is null) return null;

            context.Compra.Add(oc);
            await context.SaveChangesAsync();
            return oc;
        }

        public async Task<OrdenCompra?> UpdateOrderAsync(string Numero, OrdenCompra oc)
        {
            var existingOrder = await context.Compra.Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Numero == Numero);

            if (existingOrder is null) return null;

            existingOrder.Description = oc.Description;
            existingOrder.Fecha = oc.Fecha;
            existingOrder.Status = oc.Status;

            context.DetalleCompra.RemoveRange(existingOrder.Items);

            var newItems = new List<DetalleCompras>();
            int fila = 1;

            foreach (var item in oc.Items) 
            {
                var newItem = new DetalleCompras
                {
                    Produc_id = item.Produc_id,
                    Product_name = item.Product_name,
                    Costo = item.Costo,
                    Subtotal = item.Subtotal,
                };
                newItems.Add(newItem);
                fila++;
            }
            await context.DetalleCompra.AddRangeAsync(newItems);
            await context.SaveChangesAsync();
            return existingOrder;
        }

        public async Task<bool> DeleteOrderAsync(string numero)
        {
            var OrderDeleted = await context.Compra.FindAsync(numero);

            if (OrderDeleted is null) return false;

            context.Compra.Remove(OrderDeleted);
            await context.SaveChangesAsync();
            return true;
        }
    }
}
