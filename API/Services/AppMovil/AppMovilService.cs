using API.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared.Dtos.AppMovil;

namespace API.Services.AppMovil
{
    public class AppMovilService(ApplicationDbContext Context, IWebHostEnvironment Environment) : IAppMovilService
    {
        private readonly ApplicationDbContext context = Context;
        private readonly IWebHostEnvironment environment = Environment;

        public async Task<List<OrderPurchase>> GetOrdersPurchaseAsync()
        {
            return await context.OrderPurchase
                .Include(p => p.Items).ToListAsync();
        }

        public async Task<OrderPurchase?> GetOrderByIdAsync(string orderid)
        {
            return await context.OrderPurchase
               .Include(o => o.Items).FirstOrDefaultAsync(o => o.OrderId == orderid);
        }

        public async Task<OrderPurchase> CreateOrderAsync([FromBody] OrderPurchase order)
        {
            await context.OrderPurchase.AddAsync(order);
            await context.SaveChangesAsync();
            return order;
        }

        public async Task<OrderPurchase?> UpdateOrderAsync([FromBody] OrderPurchase order)
        {
            var existing = await context.OrderPurchase.Include(p => p.Items)
                .FirstOrDefaultAsync(p => p.OrderId == order.OrderId);

            if (existing == null)
                return null;

            existing.OrderDate = order.OrderDate;
            await context.SaveChangesAsync();
            return existing;
        }

        public async Task<OrderPurchase?> DeleteOrderAsync(string orderid)
        {
            var productDeleted = await context.OrderPurchase.Include(P => P.Items)
                .FirstOrDefaultAsync(p => p.OrderId == orderid);

            if (productDeleted == null)
                return null;

            context.OrderPurchase.Remove(productDeleted);
            await context.SaveChangesAsync();
            return productDeleted;
        }

        public Task<bool> ProcesarOrden(string orderid)
        {
            throw new NotImplementedException();
        }

        
    }
}
