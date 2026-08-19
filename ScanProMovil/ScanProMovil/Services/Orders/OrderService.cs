using Microsoft.EntityFrameworkCore;
using ScanProMovil.Data;
using ScanProMovil.Data.Entities;
using System.Diagnostics;
using System.Text.Json;

namespace ScanProMovil.Services.Orders
{
    public class OrderService : IOrderServices
    {
        private readonly AppDbContext _context;
        public IHttpClientFactory httpFactory { get; set; }
        private static readonly JsonSerializerOptions jsonOptions = new JsonSerializerOptions()
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true
        };

        public OrderService(IHttpClientFactory HttpFactory, AppDbContext context)
        {
            this.httpFactory = HttpFactory;
            _context = context;
        }

        public async Task<List<Order>> GetOrdersLocalSqliteAsync()
        {
            return await _context.Orders
                .AsNoTracking()
                .Include(o => o.Items)
                .OrderByDescending(o => o.OrderNumber)
                .ToListAsync();
        }

        public async Task<bool> SaveOrdersLocalSqliteAsync(Order order)
        {
            try
            {
                _context.Orders.Add(order);
                await _context.SaveChangesAsync();
                _context.Entry(order).State = EntityState.Detached;
                Debug.WriteLine($"Orden {order.OrderNumber} guardada con Id {order.OrderId}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("error sqlite:" + ex.Message);
                return false;
            }
        }

        public async Task<bool> DeleteOrderLocalSqliteAsync(string idorder)
        {
            try
            {
                var order = await _context.Orders
                    .FirstOrDefaultAsync(o => o.OrderNumber == idorder);
                if (order is null) return false;

                _context.Orders.Remove(order);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("error al borrar orden: " + ex.Message);
                return false;
            }
        }

        public async Task<Order> GetOrderById(string orderid)
        {
            return await _context.Orders.AsNoTracking()
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.OrderNumber == orderid) ?? new Order();
        }

        public async Task<bool> UpdateOrder(Order order)
        {
            try
            {
                _context.Orders.Update(order);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("error al actualizar orden: " + ex.Message);
                return false;
            }
        }

        public async Task<List<Product>> GetProductsRemoteApi()
        {
            var url = $"api/products/getproducts";
            var clientHttp = httpFactory.CreateClient("scanpro");
            var response = await clientHttp.GetAsync(url);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(json)) return new List<Product>();
            var products = await JsonSerializer.DeserializeAsync<List<Product>>(
                new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json)), jsonOptions);
            return (products ?? new List<Product>());
        }
    }
}
