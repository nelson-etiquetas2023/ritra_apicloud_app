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
            if (string.IsNullOrWhiteSpace(searchText)) return await GetProductsLocal();

            var term = searchText.Trim();
            var termLower = term.ToLower();
            return await _context.Products.AsNoTracking()
                .Where(p => p.Product_Name.ToLower().Contains(termLower)
                    || p.product_code.ToLower().Contains(termLower)
                    || p.CodeBar.ToLower().Contains(termLower))
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
                // Normaliza y deduplica CodeBar para no violar índice único filtrado
                var seenCodeBar = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var seenCode = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var deduped = new List<Product>(products.Count);
                foreach (var p in products)
                {
                    if (p is null) continue;
                    p.product_code = (p.product_code ?? "").Trim();
                    p.Product_Name = (p.Product_Name ?? "").Trim();
                    if (string.IsNullOrWhiteSpace(p.Product_Name)) p.Product_Name = p.product_code;
                    p.Product_Type ??= string.Empty;
                    p.Unidad ??= string.Empty;
                    p.CodeBar = (p.CodeBar ?? "").Trim().Trim('*').Trim();
                    p.Images ??= new ObservableCollection<ProductImagen>();

                    // Si product_code duplicado en el lote, conserva primera ocurrencia
                    if (!string.IsNullOrWhiteSpace(p.product_code) && !seenCode.Add(p.product_code))
                    {
                        Debug.WriteLine($"SaveLocalProducts: product_code duplicado {p.product_code} (ID {p.Product_Id}) se omite duplicado");
                        continue;
                    }

                    // CodeBar duplicado → limpia a vacío (permitido por índice filtrado)
                    if (!string.IsNullOrWhiteSpace(p.CodeBar))
                    {
                        if (!seenCodeBar.Add(p.CodeBar))
                        {
                            Debug.WriteLine($"SaveLocalProducts: CodeBar duplicado '{p.CodeBar}' en {p.product_code} (ID {p.Product_Id}) → se limpia");
                            p.CodeBar = "";
                        }
                    }

                    foreach (var img in p.Images)
                    {
                        img.ProductId = p.Product_Id;
                    }
                    deduped.Add(p);
                }
                products = deduped;

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

                // Asegura que la BD tenga el esquema actual (crea ProductImages si falta)
                try { await _context.Database.MigrateAsync(); } catch (Exception migEx) { Debug.WriteLine($"Migrate en SaveLocalProducts: {migEx.Message}"); }

                // Transacción atómica: o se reemplaza el catálogo completo o se conserva
                // el anterior. Evita quedar sin productos si el sync falla a mitad.
                using var tx = await _context.Database.BeginTransactionAsync();

                // Se limpian las tablas con SQL directo. RemoveRange+AddRange con las mismas
                // claves hace que EF "resucite" las entidades como Added y las filas viejas
                // (incluidas las de ProductImages.Id) quedan huérfanas -> UNIQUE constraint failed.
                try { await _context.Database.ExecuteSqlRawAsync("DELETE FROM ProductImages"); }
                catch (SqliteException ex) when (ex.SqliteErrorCode == 1 && ex.Message.Contains("no such table", StringComparison.OrdinalIgnoreCase))
                {
                    Debug.WriteLine($"ProductImages no existe, se ignora DELETE: {ex.Message}");
                    // Intenta crear tablas si faltan
                    try { await _context.Database.EnsureCreatedAsync(); } catch { }
                }
                await _context.Database.ExecuteSqlRawAsync("DELETE FROM Products");

                await _context.Products.AddRangeAsync(products);
                await _context.SaveChangesAsync();
                await tx.CommitAsync();
                Debug.WriteLine($"Se guardaron {products.Count} productos en la tabla local...");
                return true;
            }
            catch (DbUpdateException dbEx)
            {
                var inner = dbEx.InnerException?.Message ?? dbEx.Message;
                var full = dbEx.InnerException != null ? $"{dbEx.Message}\nInner: {dbEx.InnerException.Message}" : dbEx.Message;
                Debug.WriteLine($"⚠️ DbUpdateException al guardar productos: {full}\n{dbEx}");
                // Detecta duplicado de índice único para dar mensaje accionable
                var isUnique = inner.Contains("UNIQUE constraint failed", StringComparison.OrdinalIgnoreCase) || inner.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase);
                var msg = isUnique
                    ? $"Conflicto de índice único (probable CodeBar duplicado). Detalle: {inner}\n\nRevisa productos con mismo código de barra en la web."
                    : inner;
                await ShowAlertAsync("Error al guardar", msg);
                return false;
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
                var msg = ex.InnerException != null ? $"{ex.Message}\nInner: {ex.InnerException.Message}" : ex.Message;
                await ShowAlertAsync(
                    "Error",
                    "Ha ocurrido un error inesperado al guardar los productos en el dispositivo...\n\n" + msg);
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

        public async Task<(bool Success, string? Error)> UpdateProducts(int id, Product producto)
        {
            try
            {
                var parametros = new ParametrosUpdateProducts(id, producto);
                var url = $"api/products/updateproducts";
                var json = JsonSerializer.Serialize(parametros, jsonOptions);
                var jsonContent = new StringContent(json, Encoding.UTF8, "application/json");
                var clientHttp = httpClient.CreateClient("scanpro");
                var response = await clientHttp.PutAsync(url, jsonContent);
                var body = await response.Content.ReadAsStringAsync();
                Debug.WriteLine($"UpdateProducts PUT {url} id={id} CodeBar='{producto.CodeBar}' => {(int)response.StatusCode} {response.ReasonPhrase} Body:{body}");
                if (response.IsSuccessStatusCode) return (true, null);
                return (false, $"HTTP {(int)response.StatusCode}: {body}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"UpdateProducts exception: {ex}");
                return (false, ex.Message);
            }
        }
    }
}
