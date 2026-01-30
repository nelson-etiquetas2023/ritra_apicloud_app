using API.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared.Dtos;
using System.Runtime.InteropServices;

namespace API.Services.Products
{
    public class ProductsService : IProductsService
    {
        private readonly ApplicationDbContext context;
        public ProductsService(ApplicationDbContext context)
        {
            this.context = context;
        }

        public async Task<List<Product>> GetProductAsync()
        {
            return await context.Productos.ToListAsync();
        }

        public async Task<Product?> GetProductByIdAsync(int productId)
        {
            return await context.Productos.FindAsync(productId);
        }

        public async Task<Product> CreateproductAsync([FromBody] Product producto)
        {
            context.Productos.Add(producto);
            await context.SaveChangesAsync();
            return producto;
        }

        public async Task<Product?> UpdateProductAsync(int productId, Product producto)
        {
            var existing = await context.Productos.FindAsync(productId);
            if (existing == null) return null;
            existing.Product_Name = producto.Product_Name;
            existing.Product_Type = producto.Product_Type;
            
            await context.SaveChangesAsync();
            return existing;

        }

        public async Task<bool> DeleteProductAsync(int productId)
        {
            var producto = await context.Productos.FindAsync(productId);
            if(producto == null) return false;
            context.Productos.Remove(producto);
            await context.SaveChangesAsync();
            return true;
        }
    }
}
