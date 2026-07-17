using API.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared.Dtos;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace API.Services.Products
{
    public class ProductsService(ApplicationDbContext context, IWebHostEnvironment environment, ILogger<ProductsService> logger) : IProductsService
    {
        private readonly ApplicationDbContext context = context;
        private readonly IWebHostEnvironment _environment = environment;
        private readonly ILogger<ProductsService> _logger = logger;

        public async Task<List<Product>> GetProductAsync()
        {
            return await context.Productos
                .Include(p => p.Images)
                .ToListAsync();

        }

        public async Task<Product?> GetProductByIdAsync(int productId)
        {
            return await context.Productos
                .Include(p => p.Images)
                .FirstOrDefaultAsync(p => p.Product_id == productId);
        }

        public async Task<Product> CreateproductAsync([FromBody] Product producto)
        {
            context.Productos.Add(producto);
            await context.SaveChangesAsync();
            return producto;
        }

        public async Task<Product?> UpdateProductAsync(int productId, Product producto)
        {
            var existing = await context.Productos
                .Include(p => p.Images)
                .FirstOrDefaultAsync(p => p.Product_id == productId);
            if (existing == null) return null;

            existing.Product_Name = producto.Product_Name;
            existing.Product_Type = producto.Product_Type;
            existing.Unidad = producto.Unidad;
            existing.Codebar = producto.Codebar;
            existing.Price = producto.Price;
            //existing.Desactivado = producto.Desactivado;
            // NO modificar las imágenes aquí, se manejan por separado en UpdateProductImageAsync

            await context.SaveChangesAsync();
            return existing;
        }

        public async Task<bool> DeleteProductAsync(int productId)
        {
            var producto = await context.Productos
                .Include(p => p.Images)
                .FirstOrDefaultAsync(p => p.Product_id == productId);
            if(producto == null) return false;

            // Eliminar archivos asociados
            foreach (var image in producto.Images)
            {
                try
                {
                    var filePath = Path.Combine(_environment.ContentRootPath, "uploads", image.StoredFileName!);
                    if (File.Exists(filePath))
                        File.Delete(filePath);
                }
                catch
                {
                    // Log error si es necesario
                }
            }

            context.Productos.Remove(producto);
            await context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> AddProductImageAsync(int productId, IFormFile file, int imageIndex)
        {
            var producto = await context.Productos.FindAsync(productId);
            if (producto == null) return false;

            if (file.Length > 0)
            {
                var productImage = new ProductImage
                {
                    ProductId = productId,
                    FileName = file.FileName,
                    StoredFileName = Path.GetRandomFileName(),
                    ContentType = file.ContentType,
                    ImageIndex = imageIndex
                };

                var uploadsPath = Path.Combine(_environment.ContentRootPath, "uploads");
                if (!Directory.Exists(uploadsPath))
                {
                    Directory.CreateDirectory(uploadsPath);
                }

                var filePath = Path.Combine(uploadsPath, productImage.StoredFileName);

                await using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(fileStream);
                }

                context.Images.Add(productImage);
                await context.SaveChangesAsync();
                return true;
            }

            return false;
        }

        public async Task<bool> DeleteProductImageAsync(int imageId)
        {
            var image = await context.Images.FindAsync(imageId);
            if (image == null) return false;

            try
            {
                var filePath = Path.Combine(_environment.ContentRootPath, "uploads", image.StoredFileName!);
                if (File.Exists(filePath))
                    File.Delete(filePath);
            }
            catch
            {
                // Log error si es necesario
            }

            context.Images.Remove(image);
            await context.SaveChangesAsync();
            return true;
        }

        public async Task<Product?> CreateProductWithImagesAsync(CreateProductWithImagesRequest request)
        {
            try
            {
                // Crear el producto
                context.Productos.Add(request.Product);
                await context.SaveChangesAsync();

                // En este punto, request.Product.Product_id debe estar asignado por EF Core
                int productId = request.Product.Product_id;

                if (productId <= 0)
                {
                    _logger.LogError("Error: Product ID no fue asignado correctamente");
                    return null;
                }

                // Procesar cada imagen en base64
                foreach (var imageData in request.Images)
                {
                    if (!string.IsNullOrEmpty(imageData.Base64Data))
                    {
                        try
                        {
                            // Decodificar base64
                            var base64Index = imageData.Base64Data.IndexOf(',');
                            var cleanBase64 = base64Index >= 0 
                                ? imageData.Base64Data[(base64Index + 1)..] 
                                : imageData.Base64Data;

                            var imageBytes = Convert.FromBase64String(cleanBase64);

                            // Generar nombre seguro con Guid
                            var fileExtension = Path.GetExtension(imageData.FileName);
                            var storedFileName = $"{Guid.NewGuid()}{fileExtension}";

                            var productImage = new ProductImage
                            {
                                ProductId = productId,
                                FileName = imageData.FileName,
                                StoredFileName = storedFileName,
                                ContentType = imageData.ContentType,
                                ImageIndex = imageData.ImageIndex
                            };

                            // Guardar archivo en disco
                            var uploadsPath = Path.Combine(_environment.ContentRootPath, "uploads");
                            if (!Directory.Exists(uploadsPath))
                            {
                                Directory.CreateDirectory(uploadsPath);
                            }

                            var filePath = Path.Combine(uploadsPath, storedFileName);
                            await System.IO.File.WriteAllBytesAsync(filePath, imageBytes);

                            // Guardar referencia en BD
                            context.Images.Add(productImage);
                            _logger.LogInformation("Image added: {FileName} (Index: {Index}) for Product {ProductId}", imageData.FileName, imageData.ImageIndex, productId);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error processing image {FileName}", imageData.FileName);
                            // Continuar con la siguiente imagen si hay error
                        }
                    }
                }

                await context.SaveChangesAsync();

                // Recargar el producto con sus imágenes
                var createdProduct = await context.Productos
                    .Include(p => p.Images)
                    .FirstOrDefaultAsync(p => p.Product_id == productId);

                return createdProduct;
            }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating product with images");
                    return null;
                }
        }

        public async Task<Product?> CreateProductWithFilesAsync(Product product, IFormFileCollection files)
        {
            try
            {
                // Agregar producto
                context.Productos.Add(product);
                await context.SaveChangesAsync();

                var productId = product.Product_id;
                if (productId <= 0)
                {
                    _logger.LogError("CreateProductWithFilesAsync: Product id no asignado después de SaveChanges");
                    return null;
                }

                var uploadsPath = Path.Combine(_environment.ContentRootPath, "uploads");
                if (!Directory.Exists(uploadsPath))
                    Directory.CreateDirectory(uploadsPath);

                for (int i = 0; i < files.Count; i++)
                {
                    var file = files[i];
                    if (file == null || file.Length == 0) continue;

                    try
                    {
                        var fileExt = Path.GetExtension(file.FileName);
                        var storedFileName = $"{Guid.NewGuid()}{fileExt}";
                        var filePath = Path.Combine(uploadsPath, storedFileName);

                        await using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await file.CopyToAsync(stream);
                        }

                        var productImage = new ProductImage
                        {
                            ProductId = productId,
                            FileName = file.FileName,
                            StoredFileName = storedFileName,
                            ContentType = file.ContentType ?? "application/octet-stream",
                            ImageIndex = i
                        };

                        context.Images.Add(productImage);
                        _logger.LogInformation("CreateProductWithFilesAsync: saved image {FileName} as {StoredFileName} for product {ProductId}", file.FileName, storedFileName, productId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "CreateProductWithFilesAsync: error saving file {FileName}", file?.FileName);
                    }
                }

                await context.SaveChangesAsync();

                var created = await context.Productos
                    .Include(p => p.Images)
                    .FirstOrDefaultAsync(p => p.Product_id == productId);

                return created;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CreateProductWithFilesAsync: error general");
                return null;
            }
        }

        public async Task<bool> UpdateProductImageAsync(int productId, Base64ImageData imageData)
        {
            try
            {
                var producto = await context.Productos
                    .Include(p => p.Images)
                    .FirstOrDefaultAsync(p => p.Product_id == productId);

                if (producto == null)
                    return false;

                // Decodificar base64
                var base64Index = imageData.Base64Data.IndexOf(',');
                var cleanBase64 = base64Index >= 0 
                    ? imageData.Base64Data[(base64Index + 1)..] 
                    : imageData.Base64Data;

                var imageBytes = Convert.FromBase64String(cleanBase64);

                // Generar nombre seguro con Guid
                var fileExtension = Path.GetExtension(imageData.FileName);
                var storedFileName = $"{Guid.NewGuid()}{fileExtension}";

                // Buscar imagen existente en ese índice
                var existingImage = producto.Images?.FirstOrDefault(i => i.ImageIndex == imageData.ImageIndex);

                if (existingImage != null)
                {
                    // Eliminar archivo antiguo
                    try
                    {
                        var oldFilePath = Path.Combine(_environment.ContentRootPath, "uploads", existingImage.StoredFileName!);
                        if (File.Exists(oldFilePath))
                            File.Delete(oldFilePath);
                    }
                    catch { }

                    // Actualizar referencia
                    existingImage.FileName = imageData.FileName;
                    existingImage.StoredFileName = storedFileName;
                    existingImage.ContentType = imageData.ContentType;
                }
                else
                {
                    // Crear nueva imagen
                    var newImage = new ProductImage
                    {
                        ProductId = productId,
                        FileName = imageData.FileName,
                        StoredFileName = storedFileName,
                        ContentType = imageData.ContentType,
                        ImageIndex = imageData.ImageIndex
                    };
                    context.Images.Add(newImage);
                }

                // Guardar archivo en disco
                var uploadsPath = Path.Combine(_environment.ContentRootPath, "uploads");
                if (!Directory.Exists(uploadsPath))
                {
                    Directory.CreateDirectory(uploadsPath);
                }

                var filePath = Path.Combine(uploadsPath, storedFileName);
                await System.IO.File.WriteAllBytesAsync(filePath, imageBytes);

                await context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating product image: {ex.Message}");
                return false;
            }
        }
    }
}
