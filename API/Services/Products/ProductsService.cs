using API.Data;
using Microsoft.AspNetCore.Mvc;
using API.Storage;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Shared.Dtos;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace API.Services.Products
{
    public class ProductsService(ApplicationDbContext context, IWebHostEnvironment environment, ILogger<ProductsService> logger, IConfiguration configuration) : IProductsService
    {
        private readonly ApplicationDbContext context = context;
        private readonly IWebHostEnvironment _environment = environment;
        private readonly ILogger<ProductsService> _logger = logger;
        private readonly IConfiguration _configuration = configuration;

        private string GetUploadsPath()
        {
            var uploadsPath = ProductImageStorage.GetPath(_environment, _configuration);
            Directory.CreateDirectory(uploadsPath);
            return uploadsPath;
        }

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

            existing.Product_Code = producto.Product_Code;
            existing.Product_Name = producto.Product_Name;
            existing.Product_Type = producto.Product_Type;
            existing.Unidad = producto.Unidad;
            existing.Codebar = producto.Codebar;
            existing.Price = producto.Price;
            existing.Costo = producto.Costo;
            existing.Stock = producto.Stock;
            existing.Stock_Mix = producto.Stock_Mix;
            existing.Stock_Max = producto.Stock_Max;
            existing.StockStatus = producto.StockStatus;
            existing.Description = producto.Description;
            existing.Marca = producto.Marca;
            existing.Model = producto.Model;
            existing.PartNumber = producto.PartNumber;
            existing.SkuNumber = producto.SkuNumber;
            existing.StatusProducts = producto.StatusProducts;

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
                    var filePath = Path.Combine(GetUploadsPath(), image.StoredFileName!);
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

                try
                {
                    var uploadsPath = GetUploadsPath();

                    var filePath = Path.Combine(uploadsPath, productImage.StoredFileName);

                    await using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(fileStream);
                    }

                    _logger.LogInformation("Imagen de producto guardada: {FileName} en {FilePath} para producto {ProductId}", file.FileName, filePath, productId);

                    context.Images.Add(productImage);
                    await context.SaveChangesAsync();
                    return true;
                }
                catch (UnauthorizedAccessException ex)
                {
                    _logger.LogError(ex, "Error de permisos al guardar imagen {FileName}. Verifica los permisos de la carpeta uploads.", file.FileName);
                    return false;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al guardar imagen {FileName}", file.FileName);
                    return false;
                }
            }

            return false;
        }

        public async Task<int> BulkCreateProductsAsync(List<Product> products)
        {
            await context.Productos.AddRangeAsync(products);
            return await context.SaveChangesAsync();
        }

        public async Task<ProductImportResult> ImportFromExcelAsync(Stream excelStream)
        {
            var result = new ProductImportResult();
            var rows = new List<(int Row, Product Product)>();
            var fileCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            using var workbook = new XLWorkbook(excelStream);
            var worksheet = workbook.Worksheets.FirstOrDefault();
            if (worksheet == null)
            {
                result.Success = false;
                result.Errors.Add(new ProductImportError { Row = 0, Message = "El archivo no contiene hojas de cálculo." });
                return result;
            }

            var lastRow = worksheet.LastRowUsed();
            if (lastRow == null)
            {
                result.Success = false;
                result.Errors.Add(new ProductImportError { Row = 0, Message = "El archivo está vacío." });
                return result;
            }

            var columns = ParseHeaderRow(worksheet);
            if (columns.Code == null)
            {
                result.Success = false;
                result.Errors.Add(new ProductImportError { Row = 0, Message = "No se encontró la columna obligatoria 'product_code'. Usa la plantilla de producto." });
                return result;
            }
            if (columns.Name == null)
            {
                result.Success = false;
                result.Errors.Add(new ProductImportError { Row = 0, Message = "No se encontró la columna obligatoria 'nombre'. Usa la plantilla de producto." });
                return result;
            }

            var (firstDataRow, lastDataRow) = GetDataRange(worksheet, columns.Code.Value, lastRow.RowNumber());

            for (int i = firstDataRow; i <= lastDataRow; i++)
            {
                var code = GetCell(worksheet, i, columns.Code!.Value);
                if (string.IsNullOrWhiteSpace(code))
                    continue;

                var name = GetCell(worksheet, i, columns.Name!.Value);
                var type = columns.Type.HasValue ? GetCell(worksheet, i, columns.Type.Value) : "";
                var unit = columns.Unit.HasValue ? GetCell(worksheet, i, columns.Unit.Value) : "";
                var codebar = columns.Codebar.HasValue ? GetCell(worksheet, i, columns.Codebar.Value) : "";
                var costRaw = columns.Cost.HasValue ? GetCell(worksheet, i, columns.Cost.Value) : "";

                if (string.IsNullOrWhiteSpace(code) && string.IsNullOrWhiteSpace(name) &&
                    string.IsNullOrWhiteSpace(type) && string.IsNullOrWhiteSpace(unit) &&
                    string.IsNullOrWhiteSpace(codebar) && string.IsNullOrWhiteSpace(costRaw))
                    continue;

                if (string.IsNullOrWhiteSpace(code))
                {
                    result.Skipped++;
                    result.Errors.Add(new ProductImportError { Row = i, Message = "El código de producto es obligatorio." });
                    continue;
                }

                if (string.IsNullOrWhiteSpace(name))
                {
                    result.Skipped++;
                    result.Errors.Add(new ProductImportError { Row = i, Message = "El nombre del producto es obligatorio." });
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(costRaw) && !TryParseCost(costRaw, out _))
                {
                    result.Skipped++;
                    result.Errors.Add(new ProductImportError { Row = i, Message = $"El costo '{costRaw}' no es un número válido." });
                    continue;
                }

                if (!fileCodes.Add(code))
                {
                    result.Skipped++;
                    result.Errors.Add(new ProductImportError { Row = i, Message = $"El código '{code}' está duplicado en el archivo." });
                    continue;
                }

                TryParseCost(costRaw, out var cost);

                rows.Add((i, new Product
                {
                    Product_Code = code,
                    Product_Name = name,
                    Product_Type = type,
                    Unidad = unit,
                    Codebar = codebar,
                    Costo = cost,
                    Description = "",
                    Marca = "",
                    Model = "",
                    PartNumber = "",
                    SkuNumber = "",
                    StatusProducts = "",
                    StockStatus = ""
                }));
            }

            if (rows.Count == 0)
            {
                result.Success = true;
                return result;
            }

            await using var transaction = await context.Database.BeginTransactionAsync();
            try
            {
                const int batchSize = 500;
                for (int offset = 0; offset < rows.Count; offset += batchSize)
                {
                    var batch = rows.Skip(offset).Take(batchSize).ToList();
                    var codes = batch.Select(r => r.Product.Product_Code).ToList();

                    var existing = await context.Productos
                        .Where(p => codes.Contains(p.Product_Code))
                        .ToListAsync();
                    var byCode = existing.ToDictionary(p => p.Product_Code, p => p, StringComparer.OrdinalIgnoreCase);

                    foreach (var (_, product) in batch)
                    {
                        if (byCode.TryGetValue(product.Product_Code, out var existingProduct))
                        {
                            existingProduct.Product_Name = product.Product_Name;
                            existingProduct.Product_Type = product.Product_Type;
                            existingProduct.Unidad = product.Unidad;
                            existingProduct.Codebar = product.Codebar;
                            existingProduct.Costo = product.Costo;
                            result.Updated++;
                        }
                        else
                        {
                            context.Productos.Add(product);
                            byCode[product.Product_Code] = product;
                            result.Inserted++;
                        }
                    }

                    await context.SaveChangesAsync();
                    _logger.LogInformation("Importación: lote {Offset}-{End} procesado (insertados {Inserted}, actualizados {Updated})",
                        offset, offset + batch.Count, result.Inserted, result.Updated);
                }

                await transaction.CommitAsync();
                result.Success = true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error al importar productos: se revirtió toda la operación.");
                result.Success = false;
                result.Errors.Add(new ProductImportError { Row = 0, Message = $"Error interno al procesar el archivo: {ex.Message}" });
            }

            return result;
        }

        private enum ImportColumn
        {
            Code, Name, Type, Unit, Codebar, Cost
        }

        private sealed class ImportColumns
        {
            public int? Code { get; set; }
            public int? Name { get; set; }
            public int? Type { get; set; }
            public int? Unit { get; set; }
            public int? Codebar { get; set; }
            public int? Cost { get; set; }
        }

        private static string NormalizeHeader(string header)
        {
            var sb = new System.Text.StringBuilder();
            foreach (var ch in header)
            {
                if (char.IsLetterOrDigit(ch))
                    sb.Append(char.ToLowerInvariant(ch));
            }
            return sb.ToString();
        }

        private static readonly Dictionary<ImportColumn, string[]> ColumnAliases = new()
        {
            [ImportColumn.Code] = ["productcode", "codigoproducto", "codigo", "code", "sku"],
            [ImportColumn.Name] = ["nombre", "nombredelproducto", "productname", "name", "descripcion", "product"],
            [ImportColumn.Type] = ["categoria", "category", "tipo", "producttype", "linea"],
            [ImportColumn.Unit] = ["unidad", "unit", "un", "unidadmedida"],
            [ImportColumn.Codebar] = ["codigodebarra", "codigodebarras", "barcode", "codebar", "codigobarral"],
            [ImportColumn.Cost] = ["costo", "cost", "preciocosto", "preciodecosto", "precio"],
        };

        private static ImportColumns ParseHeaderRow(IXLWorksheet worksheet)
        {
            var headerRow = worksheet.Row(1);
            var lastCol = headerRow.LastCellUsed()?.Address.ColumnNumber ?? 0;
            var map = new Dictionary<string, ImportColumn>();

            foreach (var (column, aliases) in ColumnAliases)
            {
                foreach (var alias in aliases)
                {
                    if (!map.ContainsKey(alias))
                        map[alias] = column;
                }
            }

            var parsed = new ImportColumns();
            for (int col = 1; col <= lastCol; col++)
            {
                var raw = headerRow.Cell(col).GetString();
                var normalized = NormalizeHeader(raw);
                if (string.IsNullOrEmpty(normalized))
                    continue;

                if (map.TryGetValue(normalized, out var column))
                {
                    switch (column)
                    {
                        case ImportColumn.Code when parsed.Code is null: parsed.Code = col; break;
                        case ImportColumn.Name when parsed.Name is null: parsed.Name = col; break;
                        case ImportColumn.Type when parsed.Type is null: parsed.Type = col; break;
                        case ImportColumn.Unit when parsed.Unit is null: parsed.Unit = col; break;
                        case ImportColumn.Codebar when parsed.Codebar is null: parsed.Codebar = col; break;
                        case ImportColumn.Cost when parsed.Cost is null: parsed.Cost = col; break;
                    }
                }
            }

            return parsed;
        }

        private static (int First, int Last) GetDataRange(IXLWorksheet worksheet, int codeColumn, int lastRow)
        {
            for (int i = 2; i <= lastRow; i++)
            {
                if (!string.IsNullOrWhiteSpace(worksheet.Cell(i, codeColumn).GetString()))
                    return (i, lastRow);
            }
            return (2, 2);
        }

        private static string GetCell(IXLWorksheet worksheet, int row, int col)
        {
            return worksheet.Cell(row, col).GetString().Trim();
        }

        private static bool TryParseCost(string raw, out decimal value)
        {
            if (decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out value))
                return true;
            return decimal.TryParse(raw, NumberStyles.Any, CultureInfo.CurrentCulture, out value);
        }

        public async Task<bool> DeleteProductImageAsync(int imageId)
        {
            var image = await context.Images.FindAsync(imageId);
            if (image == null) return false;

            try
            {
                var filePath = Path.Combine(GetUploadsPath(), image.StoredFileName!);
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
                            var uploadsPath = GetUploadsPath();

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
            var savedFiles = new List<string>();
            await using var transaction = await context.Database.BeginTransactionAsync();

            try
            {
                context.Productos.Add(product);
                await context.SaveChangesAsync();

                var productId = product.Product_id;
                if (productId <= 0)
                    throw new InvalidOperationException("El identificador del producto no fue asignado.");

                var uploadsPath = GetUploadsPath();
                for (var i = 0; i < files.Count; i++)
                {
                    var file = files[i];
                    if (file.Length == 0)
                        continue;

                    var storedFileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
                    var filePath = Path.Combine(uploadsPath, storedFileName);

                    await using (var stream = new FileStream(filePath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                    {
                        await file.CopyToAsync(stream);
                    }

                    savedFiles.Add(filePath);
                    context.Images.Add(new ProductImage
                    {
                        ProductId = productId,
                        FileName = file.FileName,
                        StoredFileName = storedFileName,
                        ContentType = file.ContentType ?? "application/octet-stream",
                        ImageIndex = i
                    });
                }

                await context.SaveChangesAsync();
                await transaction.CommitAsync();

                return await context.Productos
                    .Include(p => p.Images)
                    .FirstOrDefaultAsync(p => p.Product_id == productId);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                foreach (var filePath in savedFiles)
                {
                    try { File.Delete(filePath); }
                    catch (Exception deleteException)
                    {
                        _logger.LogWarning(deleteException, "No se pudo eliminar el archivo temporal {FilePath}", filePath);
                    }
                }

                _logger.LogError(ex, "No se pudo guardar el producto ni sus imágenes. Ruta configurada: {UploadsPath}", ProductImageStorage.GetPath(_environment, _configuration));
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
                        var oldFilePath = Path.Combine(GetUploadsPath(), existingImage.StoredFileName!);
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
                var uploadsPath = GetUploadsPath();

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
