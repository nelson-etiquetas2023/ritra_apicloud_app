using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ScanProMovil.Data;
using ScanProMovil.Data.Entities;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace ScanProMovil.Services.Products
{
    public class ProductsService : IProductsService
    {
        private readonly AppDbContext _context;
        private readonly IHttpClientFactory httpClient;

        private static readonly JsonSerializerOptions jsonOptions = new JsonSerializerOptions()
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true
        };

        public ProductsService(IHttpClientFactory httpClient, AppDbContext context)
        {
            this.httpClient = httpClient;
            _context = context;
        }

        public async Task<Product?> GetProductLocalById(string codebar)
        {
            //esta busqueda es por codigo de barra.
            if (string.IsNullOrEmpty(codebar)) return null;

            return await _context.Products.AsNoTracking()
                .FirstOrDefaultAsync(p => p.CodeBar == codebar);
        }

        public async Task<Product?> GetProductLocalByCode(string code)
        {
            //busca por codigo de barra (primario) y por codigo de producto (fallback).
            if (string.IsNullOrWhiteSpace(code)) return null;

            var term = code.Trim();
            return await _context.Products.AsNoTracking()
                .FirstOrDefaultAsync(p => p.CodeBar == term || p.product_code == term);
        }

        public async Task<List<Product>> SearchProductsLocal(string searchText)
        {
            if (string.IsNullOrEmpty(searchText)) return await GetProductsLocal();

            return await _context.Products.AsNoTracking()
                .Where(p => p.Product_Name.ToLower().Contains(searchText.ToLower())
                || p.product_code.Contains(searchText) || p.CodeBar.Contains(searchText))
                .ToListAsync();
        }

        public async Task<List<Product>> GetProductsLocal()
        {
            return await _context.Products.AsNoTracking().ToListAsync();
        }

        private static readonly SemaphoreSlim _syncLock = new(1, 1);

        public async Task<bool> SaveLocalProducts(List<Product> products)
        {
            await _syncLock.WaitAsync();
            try
            {
                foreach (var p in products)
                {
                    if (p is null) continue;
                    p.product_code ??= string.Empty;
                    p.Product_Name ??= string.Empty;
                    p.Product_Type ??= string.Empty;
                    p.Unidad ??= string.Empty;
                    p.CodeBar ??= string.Empty;
                    p.Images ??= new ObservableCollection<ProductImagen>();

                    foreach (var img in p.Images)
                    {
                        img.ProductId = p.Product_Id;
                    }
                }

                // El DbContext es Singleton: se desacoplan las entidades previas
                // para evitar colisiones de clave (Product_Id) al volver a sincronizar.
                foreach (var entry in _context.ChangeTracker.Entries<Product>().ToList())
                {
                    entry.State = EntityState.Detached;
                }
                foreach (var entry in _context.ChangeTracker.Entries<ProductImagen>().ToList())
                {
                    entry.State = EntityState.Detached;
                }

                // Se limpian las tablas con SQL directo. RemoveRange+AddRange con las mismas
                // claves hace que EF "resucite" las entidades como Added y las filas viejas
                // (incluidas las de ProductImages.Id) quedan huérfanas -> UNIQUE constraint failed.
                await _context.Database.ExecuteSqlRawAsync("DELETE FROM ProductImages");
                await _context.Database.ExecuteSqlRawAsync("DELETE FROM Products");

                await _context.Products.AddRangeAsync(products);
                await _context.SaveChangesAsync();
                Debug.WriteLine($"Se guardaron {products.Count} productos en la tabla local...");
                return true;
            }
            catch (SqliteException ex)
            {
                Debug.WriteLine("⚠️***error al guardar los productos locales..., " +
                    "code error =>*** " + ex);
                await ShowAlertAsync(
                    "Error",
                    "Ha ocurrido un error al guardar los productos en el dispositivo...\n\n" + ex.Message);
                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("error al guardar los productos locales: " + ex);
                await ShowAlertAsync(
                    "Error",
                    "Ha ocurrido un error inesperado al guardar los productos en el dispositivo...\n\n" + ex.Message);
                return false;
            }
            finally
            {
                _syncLock.Release();
            }
        }

        private static async Task ShowAlertAsync(string title, string message)
        {
            var page = Application.Current?.Windows.FirstOrDefault()?.Page;
            if (page is not null)
            {
                await MainThread.InvokeOnMainThreadAsync(() => page.DisplayAlertAsync(title, message, "Aceptar"));
            }
        }

        public async Task<List<Product>?> GetProducts()
        {
            try
            {
                var url = $"api/products/getproducts";
                var clientHttp = httpClient.CreateClient("scanpro");
                var response = await clientHttp.GetAsync(url);
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrWhiteSpace(json)) return new List<Product>();
                var products = await JsonSerializer.DeserializeAsync<List<Product>>(
                    new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json)), jsonOptions);
                return (products ?? new List<Product>());
            }
            catch (Exception ex)
            {
                Debug.WriteLine("⚠️***error al sincronizar los productos de la web => " + ex.Message);
                await ShowAlertAsync(
                    "Error de sincronización",
                    "No se pudieron sincronizar los productos desde la web...\n\nDetalle: " + ex.Message);
                return null;
            }
        }

        public Product AddProducts(Product producto)
        {
            _context.Products.Add(producto);
            _context.SaveChanges();
            return producto;
        }

        public bool DeleteProducts(string productid)
        {
            var producto = _context.Products.FirstOrDefault(p => p.product_code == productid);
            if (producto is null) return false;
            _context.Products.Remove(producto);
            return _context.SaveChanges() > 0;
        }

        public Product GetPorductById(string productid)
        {
            return _context.Products.AsNoTracking()
                .FirstOrDefault(p => p.product_code == productid) ?? new Product();
        }

        public async Task<bool> UpdateProductLocal(Product producto)
        {
            if (producto is null) return false;

            var local = await _context.Products.FirstOrDefaultAsync(p => p.Product_Id == producto.Product_Id);
            if (local is null) return false;

            local.CodeBar = producto.CodeBar?.Trim() ?? "";
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> UpdateProducts(int id, Product producto)
        {
            //utilizo una tupla para pasar 2 parametros a la api.
            var parametros = new ParametrosUpdateProducts(id, producto);
            var url = $"api/products/updateproducts";
            var json = JsonSerializer.Serialize(parametros, jsonOptions);
            var jsonContent = new StringContent(json, Encoding.UTF8, "application/json");
            var clientHttp = httpClient.CreateClient("scanpro");
            var response = await clientHttp.PutAsync(url, jsonContent);
            return response.IsSuccessStatusCode;
        }
    }
}
