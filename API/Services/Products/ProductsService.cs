using API.Data;
using Microsoft.AspNetCore.Mvc;
using API.Storage;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Shared.Dtos;
using Shared.Dtos.Inventario;
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

        private string GetImagesPath()
        {
            var imagesPath = ProductImageStorage.GetPath(_environment, _configuration);
            Directory.CreateDirectory(imagesPath);
            return imagesPath;
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

        private async Task<Product?> FindProductByCodeAsync(string? code, int? excludeProductId = null)
        {
            if (string.IsNullOrWhiteSpace(code)) return null;
            var normalized = code.Trim();
            return await context.Productos.FirstOrDefaultAsync(p =>
                p.Product_Code == normalized &&
                (excludeProductId == null || p.Product_id != excludeProductId.Value));
        }

        private async Task<Product?> FindProductByCodebarAsync(string? codebar, int? excludeProductId = null)
        {
            if (string.IsNullOrWhiteSpace(codebar)) return null;
            var normalized = codebar.Trim();
            return await context.Productos.FirstOrDefaultAsync(p =>
                p.Codebar == normalized &&
                (excludeProductId == null || p.Product_id != excludeProductId.Value));
        }

        private static ServiceResponse<Product> DuplicateCodeResponse(Product existing, string code)
        {
            return new ServiceResponse<Product>
            {
                Success = false,
                Message = $"El c�digo '{code}' ya est� asignado al producto '{existing.Product_Name}' (ID {existing.Product_id}). Debe cambiarlo para registrar este producto.",
                Data = existing
            };
        }

        private static ServiceResponse<Product> DuplicateCodebarResponse(Product existing, string codebar)
        {
            return new ServiceResponse<Product>
            {
                Success = false,
                Message = $"El código de barra '{codebar}' ya está asignado al producto '{existing.Product_Name}' ({existing.Product_Code}).",
                Data = existing
            };
        }

        public async Task<ServiceResponse<Product>> CreateproductAsync([FromBody] Product producto)
        {
            var code = producto.Product_Code?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(code))
            {
                return new ServiceResponse<Product> { Success = false, Message = "El c�digo de producto es obligatorio." };
            }

            var existing = await FindProductByCodeAsync(code);
            if (existing != null)
            {
                return DuplicateCodeResponse(existing, code);
            }

            var codebar = producto.Codebar?.Trim() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(codebar))
            {
                var dupBar = await FindProductByCodebarAsync(codebar);
                if (dupBar != null)
                {
                    return DuplicateCodebarResponse(dupBar, codebar);
                }
                producto.Codebar = codebar;
            }

            producto.Product_Code = code;
            producto.Product_Name ??= "";
            producto.Product_Type ??= "";
            producto.Codebar ??= "";
            producto.Unidad ??= "";
            producto.Description ??= "";
            producto.SkuNumber ??= "";
            producto.PartNumber ??= "";
            producto.Model ??= "";
            producto.Marca ??= "";
            producto.StatusProducts ??= "";
            context.Productos.Add(producto);
            await context.SaveChangesAsync();
            return new ServiceResponse<Product> { Success = true, Data = producto };
        }

public async Task<ServiceResponse<Product>> UpdateProductAsync(int productId, Product producto)
        {
            var existing = await context.Productos
                .Include(p => p.Images)
                .FirstOrDefaultAsync(p => p.Product_id == productId);
            if (existing == null)
            {
                return new ServiceResponse<Product> { Success = false, Message = "El producto no existe." };
            }

            var code = producto.Product_Code?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(code))
            {
                return new ServiceResponse<Product> { Success = false, Message = "El c�digo de producto es obligatorio." };
            }

            var duplicate = await FindProductByCodeAsync(code, excludeProductId: productId);
            if (duplicate != null)
            {
                return DuplicateCodeResponse(duplicate, code);
            }

            // Validación atómica Codebar (idempotente: mismo producto con mismo codebar = OK)
            var codebar = producto.Codebar?.Trim() ?? "";
            if (!string.IsNullOrWhiteSpace(codebar))
            {
                // Idempotencia: si ya tiene ese codebar, no buscar duplicado
                if (!string.Equals(existing.Codebar?.Trim(), codebar, StringComparison.OrdinalIgnoreCase))
                {
                    var dupBar = await FindProductByCodebarAsync(codebar, excludeProductId: productId);
                    if (dupBar != null)
                        return DuplicateCodebarResponse(dupBar, codebar);
                }
            }

            // Solo se actualiza Codebar - no tocar resto de campos (requisito móvil)
            existing.Codebar = codebar;

            await context.SaveChangesAsync();
            return new ServiceResponse<Product> { Success = true, Data = existing };
        }

        public async Task<ServiceResponse<Product>> UpdateProductFullAsync(int productId, Product producto)
        {
            var existing = await context.Productos
                .Include(p => p.Images)
                .FirstOrDefaultAsync(p => p.Product_id == productId);
            if (existing == null)
            {
                return new ServiceResponse<Product> { Success = false, Message = "El producto no existe." };
            }

            var code = producto.Product_Code?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(code))
            {
                return new ServiceResponse<Product> { Success = false, Message = "El código de producto es obligatorio." };
            }

            var duplicate = await FindProductByCodeAsync(code, excludeProductId: productId);
            if (duplicate != null)
            {
                return DuplicateCodeResponse(duplicate, code);
            }

            var codebar = producto.Codebar?.Trim() ?? "";
            if (!string.IsNullOrWhiteSpace(codebar))
            {
                if (!string.Equals(existing.Codebar?.Trim(), codebar, StringComparison.OrdinalIgnoreCase))
                {
                    var dupBar = await FindProductByCodebarAsync(codebar, excludeProductId: productId);
                    if (dupBar != null)
                        return DuplicateCodebarResponse(dupBar, codebar);
                }
            }

            // Edición completa WEB. No se toca Stock (lo mueve el ledger de inventario)
            // ni Images/IsActive (tienen sus endpoints).
            existing.Product_Code = code;
            existing.Product_Name = producto.Product_Name?.Trim() ?? existing.Product_Name;
            existing.Product_Type = producto.Product_Type?.Trim() ?? existing.Product_Type;
            existing.Price = producto.Price;
            existing.Codebar = codebar;
            existing.Unidad = producto.Unidad?.Trim() ?? existing.Unidad;
            existing.Costo = producto.Costo;
            existing.Stock_Mix = producto.Stock_Mix;
            existing.Stock_Max = producto.Stock_Max;
            existing.ManejaVencimiento = producto.ManejaVencimiento;
            existing.ManejaSeriales = producto.ManejaSeriales;
            existing.ManejaLotes = producto.ManejaLotes;
            existing.FechaVencimiento = producto.ManejaVencimiento ? producto.FechaVencimiento : null;
            existing.Description = producto.Description?.Trim() ?? "";
            existing.SkuNumber = producto.SkuNumber?.Trim() ?? "";
            existing.PartNumber = producto.PartNumber?.Trim() ?? "";
            existing.Model = producto.Model?.Trim() ?? "";
            existing.Marca = producto.Marca?.Trim() ?? "";
            existing.StatusProducts = producto.StatusProducts ?? existing.StatusProducts;

            await context.SaveChangesAsync();
            return new ServiceResponse<Product> { Success = true, Data = existing };
        }

        public async Task<bool> SetProductActiveAsync(int productId, bool active)
        {
            var existing = await context.Productos.FindAsync(productId);
            if (existing == null) return false;

            existing.IsActive = active;
            await context.SaveChangesAsync();
            return true;
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
                    var filePath = Path.Combine(GetImagesPath(), image.StoredFileName!);
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
                    var imagesPath = GetImagesPath();

                    var filePath = Path.Combine(imagesPath, productImage.StoredFileName);

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
                    _logger.LogError(ex, "Error de permisos al guardar imagen {FileName}. Verifica los permisos de la carpeta de im�genes.", file.FileName);
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
                result.Errors.Add(new ProductImportError { Row = 0, Message = "El archivo no contiene hojas de c�lculo." });
                return result;
            }

            var lastRow = worksheet.LastRowUsed();
            if (lastRow == null)
            {
                result.Success = false;
                result.Errors.Add(new ProductImportError { Row = 0, Message = "El archivo est� vac�o." });
                return result;
            }

            var columns = ParseHeaderRow(worksheet);
            if (columns.Code == null)
            {
                result.Success = false;
                result.Errors.Add(new ProductImportError { Row = 0, Message = "No se encontr� la columna obligatoria 'product_code'. Usa la plantilla de producto." });
                return result;
            }
            if (columns.Name == null)
            {
                result.Success = false;
                result.Errors.Add(new ProductImportError { Row = 0, Message = "No se encontr� la columna obligatoria 'nombre'. Usa la plantilla de producto." });
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
                    result.Errors.Add(new ProductImportError { Row = i, Message = "El c�digo de producto es obligatorio." });
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
                    result.Errors.Add(new ProductImportError { Row = i, Message = $"El costo '{costRaw}' no es un n�mero v�lido." });
                    continue;
                }

                if (!fileCodes.Add(code))
                {
                    result.Skipped++;
                    result.Errors.Add(new ProductImportError { Row = i, Message = $"El c�digo '{code}' est� duplicado en el archivo." });
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
                    _logger.LogInformation("Importaci�n: lote {Offset}-{End} procesado (insertados {Inserted}, actualizados {Updated})",
                        offset, offset + batch.Count, result.Inserted, result.Updated);
                }

                await transaction.CommitAsync();
                result.Success = true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error al importar productos: se revirti� toda la operaci�n.");
                result.Success = false;
                result.Errors.Add(new ProductImportError { Row = 0, Message = $"Error interno al procesar el archivo: {ex.Message}" });
            }

            return result;
        }

        public byte[] GenerateStockTemplate(bool incluirProductos)
        {
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("CargaInicial");

            string[] headers = ["product_id", "product_code", "product_name", "categoria", "unidad", "stock_actual", "stock_inicial"];
            for (int i = 0; i < headers.Length; i++)
                ws.Cell(1, i + 1).Value = headers[i];
            ws.Row(1).Style.Font.Bold = true;
            ws.Row(1).Style.Fill.BackgroundColor = XLColor.FromHtml("#147a3d");
            ws.Row(1).Style.Font.FontColor = XLColor.White;
            ws.SheetView.FreezeRows(1);

            if (incluirProductos)
            {
                var productos = context.Productos
                    .AsNoTracking()
                    .OrderBy(p => p.Product_Code)
                    .Select(p => new { p.Product_id, p.Product_Code, p.Product_Name, p.Product_Type, p.Unidad, p.Stock })
                    .ToList();

                int row = 2;
                foreach (var p in productos)
                {
                    ws.Cell(row, 1).Value = p.Product_id;
                    ws.Cell(row, 2).Value = p.Product_Code;
                    ws.Cell(row, 3).Value = p.Product_Name;
                    ws.Cell(row, 4).Value = p.Product_Type;
                    ws.Cell(row, 5).Value = p.Unidad;
                    ws.Cell(row, 6).Value = p.Stock;
                    ws.Cell(row, 7).Value = 0;
                    row++;
                }
            }

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        public async Task<StockInicialResult> AplicarStockInicialAsync(Stream excelStream, string usuario)
        {
            var result = new StockInicialResult();
            usuario = string.IsNullOrWhiteSpace(usuario) ? "Sistema" : usuario.Trim();

            using var workbook = new XLWorkbook(excelStream);
            var ws = workbook.Worksheets.FirstOrDefault();
            if (ws == null)
            {
                result.Errors.Add(new StockInicialError { Row = 0, Message = "El archivo no contiene hojas de cálculo." });
                return result;
            }

            var lastRow = ws.LastRowUsed();
            if (lastRow == null)
            {
                result.Errors.Add(new StockInicialError { Row = 0, Message = "El archivo está vacío." });
                return result;
            }

            int codeCol = 0, stockCol = 0;
            var header = ws.Row(1);
            var lastCol = header.LastCellUsed()?.Address.ColumnNumber ?? 0;
            for (int c = 1; c <= lastCol; c++)
            {
                var h = NormalizeHeader(header.Cell(c).GetString());
                if (h is "productcode" or "codigoproducto" or "codigo" or "code" or "sku")
                {
                    if (codeCol == 0) codeCol = c;
                }
                else if (h is "stockinicial" or "stock" or "cantidad" or "inicial" or "existencia")
                {
                    if (stockCol == 0) stockCol = c;
                }
            }
            if (codeCol == 0)
            {
                result.Errors.Add(new StockInicialError { Row = 0, Message = "No se encontró la columna 'product_code'. Usa la plantilla." });
                return result;
            }
            if (stockCol == 0)
            {
                result.Errors.Add(new StockInicialError { Row = 0, Message = "No se encontró la columna 'stock_inicial'. Usa la plantilla." });
                return result;
            }

            var (first, last) = GetDataRange(ws, codeCol, lastRow.RowNumber());
            var vistos = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var lineas = new List<StockInicialLinea>();
            for (int i = first; i <= last; i++)
            {
                var code = GetCell(ws, i, codeCol);
                var stockRaw = GetCell(ws, i, stockCol);
                if (string.IsNullOrWhiteSpace(code) && string.IsNullOrWhiteSpace(stockRaw))
                    continue;
                if (string.IsNullOrWhiteSpace(code))
                {
                    result.Errors.Add(new StockInicialError { Row = i, Message = "El código de producto es obligatorio." });
                    continue;
                }
                if (string.IsNullOrWhiteSpace(stockRaw))
                {
                    // Vacío vale 0: no se aplica ni da error.
                    result.Omitidos++;
                    continue;
                }
                if (!vistos.Add(code))
                {
                    result.Errors.Add(new StockInicialError { Row = i, Message = $"El código '{code}' está duplicado en el archivo." });
                    continue;
                }
                if (!TryParseStock(stockRaw, out var stock) || stock < 0)
                {
                    result.Errors.Add(new StockInicialError { Row = i, Message = $"El stock inicial '{stockRaw}' no es un número válido mayor o igual a cero." });
                    continue;
                }
                if (stock == 0)
                {
                    // Solo se toman los mayores a cero.
                    result.Omitidos++;
                    continue;
                }
                lineas.Add(new StockInicialLinea { Row = i, Codigo = code, StockInicial = stock });
            }

            if (result.Errors.Count > 0)
            {
                result.Message = "La carga tiene errores y no se aplicó ningún cambio.";
                return result;
            }
            if (lineas.Count == 0)
            {
                result.Errors.Add(new StockInicialError { Row = 0, Message = "El archivo no tiene filas con datos." });
                return result;
            }

            var yaExiste = await context.AjustesInventario.AnyAsync(a => a.Motivo == MotivoAjuste.Inicial);
            if (yaExiste)
            {
                result.Message = "Ya existe una carga inicial aplicada. Solo un admin puede reabrirla.";
                result.Errors.Add(new StockInicialError { Row = 0, Message = result.Message });
                return result;
            }

            var codigos = lineas.Select(l => l.Codigo).ToList();
            var productos = await context.Productos
                .Where(p => codigos.Contains(p.Product_Code) ||
                    (p.Codebar != null && codigos.Contains(p.Codebar)))
                .ToListAsync();
            var porCodigo = new Dictionary<string, Product>(StringComparer.OrdinalIgnoreCase);
            foreach (var p in productos)
            {
                if (!string.IsNullOrWhiteSpace(p.Product_Code)) porCodigo.TryAdd(p.Product_Code.Trim(), p);
                if (!string.IsNullOrWhiteSpace(p.Codebar)) porCodigo.TryAdd(p.Codebar.Trim(), p);
            }
            foreach (var l in lineas)
            {
                if (!porCodigo.ContainsKey(l.Codigo))
                    result.Errors.Add(new StockInicialError { Row = l.Row, Message = $"El código '{l.Codigo}' no existe en el sistema." });
            }
            if (result.Errors.Count > 0)
            {
                result.Message = "Hay códigos inexistentes y no se aplicó ningún cambio.";
                return result;
            }

            var lote = $"INICIAL-{DateTime.Now:yyyyMMddHHmmss}";
            await using var tx = await context.Database.BeginTransactionAsync();
            try
            {
                foreach (var l in lineas)
                {
                    var p = porCodigo[l.Codigo];
                    var anterior = p.Stock;
                    var nuevo = l.StockInicial;
                    await context.Productos
                        .Where(x => x.Product_id == p.Product_id)
                        .ExecuteUpdateAsync(s => s.SetProperty(x => x.Stock, nuevo));
                    context.AjustesInventario.Add(new AjusteInventario
                    {
                        Fecha = DateTime.Now,
                        Usuario = usuario,
                        Lote = lote,
                        ProductoId = p.Product_id,
                        Codigo = l.Codigo,
                        StockAnterior = anterior,
                        StockNuevo = nuevo,
                        Motivo = MotivoAjuste.Inicial
                    });
                }
                await context.SaveChangesAsync();
                await tx.CommitAsync();
                result.Success = true;
                result.Lote = lote;
                result.Aplicados = lineas.Count;
                result.Message = $"Carga inicial {lote} aplicada: {lineas.Count} productos." +
                    (result.Omitidos > 0 ? $" Se omitieron {result.Omitidos} filas sin stock por llenar." : "");
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                _logger.LogError(ex, "Error al aplicar carga inicial de stock.");
                result.Message = $"Error interno al aplicar la carga: {ex.Message}";
                result.Errors.Add(new StockInicialError { Row = 0, Message = result.Message });
            }
            return result;
        }

        private static bool TryParseStock(string raw, out double value)
        {
            if (double.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out value))
                return true;
            return double.TryParse(raw, NumberStyles.Any, CultureInfo.CurrentCulture, out value);
        }

        public async Task<List<CargaInicialResumen>> GetCargasInicialesAsync()
        {
            return await context.AjustesInventario
                .Where(a => a.Motivo == MotivoAjuste.Inicial)
                .GroupBy(a => new { a.Lote, a.Usuario })
                .Select(g => new CargaInicialResumen
                {
                    Lote = g.Key.Lote,
                    Fecha = g.Max(a => a.Fecha),
                    Usuario = g.Key.Usuario,
                    Productos = g.Count(),
                    Comentario = g.Select(a => a.Comentario).FirstOrDefault() ?? ""
                })
                .OrderByDescending(r => r.Fecha)
                .ToListAsync();
        }

        public async Task<List<AjusteInventario>> GetCargaDetalleAsync(string lote)
        {
            var filas = await context.AjustesInventario
                .Where(a => a.Lote == lote && a.Motivo == MotivoAjuste.Inicial)
                .OrderBy(a => a.Codigo)
                .ToListAsync();
            if (filas.Count == 0) return filas;
            var ids = filas.Select(a => a.ProductoId).Distinct().ToList();
            var nombres = await context.Productos
                .Where(p => ids.Contains(p.Product_id))
                .Select(p => new { p.Product_id, p.Product_Name })
                .ToListAsync();
            var porId = nombres.ToDictionary(p => p.Product_id, p => p.Product_Name ?? "");
            foreach (var a in filas)
            {
                if (porId.TryGetValue(a.ProductoId, out var n)) a.ProductoNombre = n;
            }
            return filas;
        }

        public async Task<StockInicialResult> ActualizarCargaInicialAsync(string lote, DateTime fecha, string comentario)
        {
            var result = new StockInicialResult { Lote = lote ?? "" };
            if (string.IsNullOrWhiteSpace(lote))
            {
                result.Message = "Indica el lote a editar.";
                return result;
            }
            var filas = await context.AjustesInventario
                .Where(a => a.Lote == lote && a.Motivo == MotivoAjuste.Inicial)
                .ToListAsync();
            if (filas.Count == 0)
            {
                result.Message = $"No existe carga inicial con lote {lote}.";
                return result;
            }
            foreach (var a in filas)
            {
                a.Fecha = fecha;
                a.Comentario = (comentario ?? "").Trim();
            }
            await context.SaveChangesAsync();
            result.Success = true;
            result.Aplicados = filas.Count;
            result.Message = $"Carga {lote} actualizada.";
            return result;
        }

        public async Task<StockInicialResult> RevertirCargaInicialAsync(string lote)
        {
            var result = new StockInicialResult { Lote = lote ?? "" };
            if (string.IsNullOrWhiteSpace(lote))
            {
                result.Message = "Indica el lote a revertir.";
                return result;
            }
            var filas = await context.AjustesInventario
                .Where(a => a.Lote == lote && a.Motivo == MotivoAjuste.Inicial)
                .ToListAsync();
            if (filas.Count == 0)
            {
                result.Message = $"No existe carga inicial con lote {lote}.";
                return result;
            }
            await using var tx = await context.Database.BeginTransactionAsync();
            try
            {
                foreach (var a in filas)
                {
                    var anterior = a.StockAnterior;
                    await context.Productos
                        .Where(p => p.Product_id == a.ProductoId)
                        .ExecuteUpdateAsync(s => s.SetProperty(p => p.Stock, anterior));
                }
                context.AjustesInventario.RemoveRange(filas);
                await context.SaveChangesAsync();
                await tx.CommitAsync();
                result.Success = true;
                result.Aplicados = filas.Count;
                result.Message = $"Carga {lote} revertida: {filas.Count} productos volvieron a su stock anterior.";
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                _logger.LogError(ex, "Error al revertir carga inicial {Lote}.", lote);
                result.Message = $"Error interno al revertir: {ex.Message}";
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
                var filePath = Path.Combine(GetImagesPath(), image.StoredFileName!);
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

        public async Task<ServiceResponse<Product>> CreateProductWithImagesAsync(CreateProductWithImagesRequest request)
        {
            try
            {
                if (request?.Product == null)
                {
                    return new ServiceResponse<Product> { Success = false, Message = "Datos del producto inv�lidos." };
                }

                var code = request.Product.Product_Code?.Trim() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(code))
                {
                    return new ServiceResponse<Product> { Success = false, Message = "El c�digo de producto es obligatorio." };
                }

                var existing = await FindProductByCodeAsync(code);
                if (existing != null)
                {
                    return DuplicateCodeResponse(existing, code);
                }

                request.Product.Product_Code = code;

                // Crear el producto
                context.Productos.Add(request.Product);
                await context.SaveChangesAsync();

                // En este punto, request.Product.Product_id debe estar asignado por EF Core
                int productId = request.Product.Product_id;

                if (productId <= 0)
                {
                    _logger.LogError("Error: Product ID no fue asignado correctamente");
                    return new ServiceResponse<Product> { Success = false, Message = "No se pudo asignar el identificador del producto." };
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
                            var imagesPath = GetImagesPath();

                            var filePath = Path.Combine(imagesPath, storedFileName);
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

                // Recargar el producto con sus im�genes
                var createdProduct = await context.Productos
                    .Include(p => p.Images)
                    .FirstOrDefaultAsync(p => p.Product_id == productId);

                return new ServiceResponse<Product> { Success = true, Data = createdProduct };
            }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating product with images");
                    return new ServiceResponse<Product> { Success = false, Message = $"Error al crear el producto: {ex.Message}" };
                }
        }

        public async Task<ServiceResponse<Product>> CreateProductWithFilesAsync(Product product, IFormFileCollection files)
        {
            var savedFiles = new List<string>();

            if (product is null)
            {
                return new ServiceResponse<Product> { Success = false, Message = "El producto es obligatorio." };
            }

            var code = product.Product_Code?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(code))
            {
                return new ServiceResponse<Product> { Success = false, Message = "El c�digo de producto es obligatorio." };
            }

            var existing = await FindProductByCodeAsync(code);
            if (existing != null)
            {
                return DuplicateCodeResponse(existing, code);
            }

            var codebarFiles = product.Codebar?.Trim() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(codebarFiles))
            {
                var dupBarFiles = await FindProductByCodebarAsync(codebarFiles);
                if (dupBarFiles != null)
                {
                    return DuplicateCodebarResponse(dupBarFiles, codebarFiles);
                }
                product.Codebar = codebarFiles;
            }

            product!.Product_Code = code;
            product.Product_Name ??= "";
            product.Product_Type ??= "";
            product.Codebar ??= "";
            product.Unidad ??= "";
            product.Description ??= "";
            product.SkuNumber ??= "";
            product.PartNumber ??= "";
            product.Model ??= "";
            product.Marca ??= "";
            product.StatusProducts ??= "";

            await using var transaction = await context.Database.BeginTransactionAsync();

            try
            {
                context.Productos.Add(product);
                await context.SaveChangesAsync();

                var productId = product.Product_id;
                if (productId <= 0)
                    throw new InvalidOperationException("El identificador del producto no fue asignado.");

                var imagesPath = GetImagesPath();
                for (var i = 0; i < files.Count; i++)
                {
                    var file = files[i];
                    if (file.Length == 0)
                        continue;

                    var storedFileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
                    var filePath = Path.Combine(imagesPath, storedFileName);

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

                return new ServiceResponse<Product>
                {
                    Success = true,
                    Data = await context.Productos
                        .Include(p => p.Images)
                        .FirstOrDefaultAsync(p => p.Product_id == productId)
                };
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

                _logger.LogError(ex, "No se pudo guardar el producto ni sus im�genes. Ruta configurada: {ImagesPath}", ProductImageStorage.GetPath(_environment, _configuration));
                return new ServiceResponse<Product> { Success = false, Message = $"Error al guardar el producto: {ex.Message}" };
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

                // Buscar imagen existente en ese �ndice
                var existingImage = producto.Images?.FirstOrDefault(i => i.ImageIndex == imageData.ImageIndex);

                if (existingImage != null)
                {
                    // Eliminar archivo antiguo
                    try
                    {
                        var oldFilePath = Path.Combine(GetImagesPath(), existingImage.StoredFileName!);
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
                var imagesPath = GetImagesPath();

                var filePath = Path.Combine(imagesPath, storedFileName);
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
