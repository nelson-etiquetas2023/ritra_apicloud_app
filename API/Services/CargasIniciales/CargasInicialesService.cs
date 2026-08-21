using API.Data;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Shared.Dtos;
using Shared.Dtos.CargasIniciales;
using System.Globalization;

namespace API.Services.CargasIniciales
{
    public class CargasInicialesService(ApplicationDbContext context) : ICargasInicialesService
    {
        private readonly ApplicationDbContext context = context;

        public async Task<List<Inicial>> GetAllAsync()
        {
            return await context.CargasIniciales
                .Include(i => i.Detalles)
                .OrderByDescending(i => i.FechaCreacion)
                .ToListAsync();
        }

        public async Task<Inicial?> GetByIdAsync(int id)
        {
            return await context.CargasIniciales
                .Include(i => i.Detalles)
                .FirstOrDefaultAsync(i => i.Id == id);
        }

        public async Task<CargaInicialSaveResult> CreateAsync(Inicial inicial)
        {
            var result = new CargaInicialSaveResult();

            if (inicial.Detalles == null || inicial.Detalles.Count == 0)
            {
                result.Success = false;
                result.Message = "El documento no tiene líneas de detalle.";
                return result;
            }

            var catalogo = await ObtenerCatalogoCodigosAsync();
            var errores = new List<RowError>();
            foreach (var detalle in inicial.Detalles)
            {
                if (string.IsNullOrWhiteSpace(detalle.ProductCode))
                {
                    errores.Add(new RowError { Row = 0, Message = "El código de producto es obligatorio en cada línea." });
                    continue;
                }

                if (!catalogo.Contains(detalle.ProductCode))
                {
                    errores.Add(new RowError { Row = 0, Message = $"El producto '{detalle.ProductCode}' no existe en el catálogo." });
                    continue;
                }

                if (detalle.Cantidad <= 0)
                {
                    errores.Add(new RowError { Row = 0, Message = $"La cantidad del producto '{detalle.ProductCode}' debe ser mayor a cero." });
                }
            }

            if (errores.Count > 0)
            {
                result.Success = false;
                result.Message = "El documento no se guardó. Corrige los errores del detalle.";
                result.Errors = errores;
                return result;
            }

            if (string.IsNullOrWhiteSpace(inicial.Numero))
                inicial.Numero = BuildNextNum();
            inicial.FechaCreacion = DateTime.Now;
            inicial.Status = 0;

            foreach (var detalle in inicial.Detalles)
            {
                detalle.Id = 0;
                detalle.Procesado = false;
                detalle.FechaProcesado = null;
            }

            context.CargasIniciales.Add(inicial);
            await context.SaveChangesAsync();

            result.Success = true;
            result.Message = $"Carga inicial {inicial.Numero} guardada correctamente.";
            result.Data = inicial;
            return result;
        }

        public async Task<CargaInicialSaveResult> UpdateAsync(int id, Inicial inicial)
        {
            var result = new CargaInicialSaveResult();

            var existing = await context.CargasIniciales
                .Include(i => i.Detalles)
                .FirstOrDefaultAsync(i => i.Id == id);
            if (existing == null)
            {
                result.Success = false;
                result.Message = $"La carga inicial {id} no fue encontrada.";
                return result;
            }

            if (existing.Status == 4)
            {
                result.Success = false;
                result.Message = "El documento ya fue procesado y no puede editarse.";
                return result;
            }

            if (inicial.Detalles == null || inicial.Detalles.Count == 0)
            {
                result.Success = false;
                result.Message = "El documento no tiene líneas de detalle.";
                return result;
            }

            var catalogo = await ObtenerCatalogoCodigosAsync();
            var errores = new List<RowError>();
            foreach (var detalle in inicial.Detalles)
            {
                if (string.IsNullOrWhiteSpace(detalle.ProductCode))
                {
                    errores.Add(new RowError { Row = 0, Message = "El código de producto es obligatorio en cada línea." });
                    continue;
                }

                if (!catalogo.Contains(detalle.ProductCode))
                {
                    errores.Add(new RowError { Row = 0, Message = $"El producto '{detalle.ProductCode}' no existe en el catálogo." });
                    continue;
                }

                if (detalle.Cantidad <= 0)
                {
                    errores.Add(new RowError { Row = 0, Message = $"La cantidad del producto '{detalle.ProductCode}' debe ser mayor a cero." });
                }
            }

            if (errores.Count > 0)
            {
                result.Success = false;
                result.Message = "El documento no se actualizó. Corrige los errores del detalle.";
                result.Errors = errores;
                return result;
            }

            existing.Numero = inicial.Numero;
            existing.Comentario = inicial.Comentario;
            existing.Status = 1;

            // Merge por ProductCode: actualiza los que llegan, agrega los nuevos
            // y conserva los detalles ya existentes (sync parcial desde la móvil).
            var incoming = inicial.Detalles.ToList();
            foreach (var detalle in incoming)
            {
                var match = existing.Detalles.FirstOrDefault(d =>
                    string.Equals(d.ProductCode?.Trim(), detalle.ProductCode?.Trim(), StringComparison.OrdinalIgnoreCase));

                if (match is not null)
                {
                    match.ProductName = detalle.ProductName;
                    match.Cantidad = detalle.Cantidad;
                    match.CantidadFisica = detalle.CantidadFisica;
                    match.Ubicacion = detalle.Ubicacion;
                    match.Costo = detalle.Costo;
                    match.Categoria = detalle.Categoria;
                    match.Unidad = detalle.Unidad;
                    match.Nota = detalle.Nota;
                }
                else
                {
                    detalle.Id = 0;
                    detalle.InicialId = id;
                    detalle.Procesado = false;
                    detalle.FechaProcesado = null;
                    context.CargasInicialesDetalles.Add(detalle);
                }
            }

            await context.SaveChangesAsync();

            result.Success = true;
            result.Message = $"Carga inicial {existing.Numero} actualizada correctamente.";
            result.Data = await GetByIdAsync(id);
            return result;
        }

        public async Task<CargaInicialSaveResult> ProcesarInicialAsync(int id)
        {
            var result = new CargaInicialSaveResult();

            var inicial = await context.CargasIniciales
                .Include(i => i.Detalles)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (inicial == null)
            {
                result.Success = false;
                result.Message = $"La carga inicial {id} no fue encontrada.";
                return result;
            }

            if (inicial.Status == 4)
            {
                result.Success = false;
                result.Message = "El documento ya fue procesado y no puede volver a procesarse.";
                return result;
            }

            bool hayFallo = false;

            foreach (var detalle in inicial.Detalles)
            {
                if (detalle.Procesado) continue;

                var producto = await ResolverProductoAsync(detalle.ProductCode);
                if (producto == null)
                {
                    hayFallo = true;
                    result.Errors.Add(new RowError { Row = 0, Message = $"Producto '{detalle.ProductCode}' no encontrado en el catálogo." });
                    continue;
                }

                producto.Stock += detalle.Cantidad;
                detalle.Procesado = true;
                detalle.FechaProcesado = DateTime.Now;
                await context.SaveChangesAsync();
            }

            inicial.Status = hayFallo ? 5 : 4;
            await context.SaveChangesAsync();

            result.Success = !hayFallo;
            result.Message = hayFallo
                ? $"El documento quedó en Transacción Fallida. Los productos con error no afectaron el inventario y el documento puede volver a procesarse."
                : $"El documento {inicial.Numero} fue procesado exitosamente. El stock de los productos se incrementó.";
            result.Data = await GetByIdAsync(id);
            return result;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var inicial = await context.CargasIniciales.FirstOrDefaultAsync(i => i.Id == id);
            if (inicial == null) return false;

            context.CargasIniciales.Remove(inicial);
            await context.SaveChangesAsync();
            return true;
        }

        public async Task<CargaInicialImportResult> ImportFromExcelAsync(Stream excelStream)
        {
            var result = new CargaInicialImportResult();
            var detalles = new List<DetalleInicial>();

            using var workbook = new XLWorkbook(excelStream);
            var worksheet = workbook.Worksheets.FirstOrDefault();
            if (worksheet == null)
            {
                result.Success = false;
                result.Errors.Add(new RowError { Row = 0, Message = "El archivo no contiene hojas de cálculo." });
                return result;
            }

            var lastDataRow = worksheet.LastRowUsed()?.RowNumber() ?? 1;

            //Catálogo de productos existentes (match por Codebar o Product_Code, case-insensitive).
            var productosExistentes = await context.Productos
                .Select(p => new { p.Codebar, p.Product_Code })
                .ToListAsync();

            var codigosExistentes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var prod in productosExistentes)
            {
                if (!string.IsNullOrWhiteSpace(prod.Codebar))
                    codigosExistentes.Add(prod.Codebar.Trim());
                if (!string.IsNullOrWhiteSpace(prod.Product_Code))
                    codigosExistentes.Add(prod.Product_Code.Trim());
            }

            for (int row = 2; row <= lastDataRow; row++)
            {
                var productCode = GetCell(worksheet, row, 1);
                var productName = GetCell(worksheet, row, 2);
                var cantidad = GetCell(worksheet, row, 3);
                var ubicacion = GetCell(worksheet, row, 4);
                var costo = GetCell(worksheet, row, 5);
                var categoria = GetCell(worksheet, row, 6);
                var unidad = GetCell(worksheet, row, 7);
                var nota = GetCell(worksheet, row, 8);

                if (string.IsNullOrWhiteSpace(productCode) && string.IsNullOrWhiteSpace(productName) &&
                    string.IsNullOrWhiteSpace(cantidad) && string.IsNullOrWhiteSpace(ubicacion))
                    continue;

                if (string.IsNullOrWhiteSpace(productCode))
                {
                    result.Skipped++;
                    result.Errors.Add(new RowError { Row = row, Message = "El código de producto es obligatorio." });
                    continue;
                }

                if (!codigosExistentes.Contains(productCode))
                {
                    result.Skipped++;
                    result.Errors.Add(new RowError { Row = row, Message = $"El producto '{productCode}' no existe en el catálogo de productos." });
                    continue;
                }

                if (!int.TryParse(cantidad, NumberStyles.Integer, CultureInfo.InvariantCulture, out var cantidadParsed))
                    cantidadParsed = 0;

                if (cantidadParsed <= 0)
                {
                    result.Skipped++;
                    result.Errors.Add(new RowError { Row = row, Message = $"La cantidad '{cantidad}' debe ser un número entero mayor a cero." });
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(costo) && !TryParseDecimal(costo, out _))
                {
                    result.Skipped++;
                    result.Errors.Add(new RowError { Row = row, Message = $"El costo '{costo}' no es un número válido." });
                    continue;
                }

                TryParseDecimal(costo, out var costoParsed);

                detalles.Add(new DetalleInicial
                {
                    ProductCode = productCode,
                    ProductName = productName,
                    Cantidad = cantidadParsed,
                    CantidadFisica = 0,
                    Ubicacion = ubicacion,
                    Costo = costoParsed,
                    Categoria = categoria,
                    Unidad = unidad,
                    Nota = nota
                });
            }

            if (detalles.Count == 0)
            {
                result.Success = true;
                return result;
            }

            result.Detalles = detalles;
            result.Inserted = detalles.Count;
            result.Success = true;
            return result;
        }

        public async Task<List<Inicial>> GetDocumentsInitialsInventoryAsync()
        {
            var scanProducts = await context.ScanProducts
                .OrderBy(p => p.OrdenId)
                .ToListAsync();

            var documentos = scanProducts
                .GroupBy(p => p.OrdenId)
                .Select(g => new Inicial
                {
                    Id = 0,
                    Numero = g.Key ?? "SIN-DOC",
                    FechaCreacion = g.Max(p => p.DateScan),
                    Comentario = $"Documento de inventario escaneado en Zebra ({g.Count()} ítems)",
                    Detalles = g.Select(p => new DetalleInicial
                    {
                        ProductCode = p.Codebar ?? "",
                        ProductName = p.ProductName ?? "",
                        Cantidad = 0,
                        CantidadFisica = p.Quantity,
                        Ubicacion = p.Ubicacion ?? "",
                        Categoria = p.Category ?? "",
                        Unidad = p.Unidad ?? "",
                        Nota = p.Estado ?? ""
                    }).ToList()
                })
                .OrderByDescending(d => d.FechaCreacion)
                .ToList();

            return documentos;
        }

        public byte[] GenerateTemplate()
        {            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("CargaInicial");

            string[] columns = ["product_code", "product_name", "cantidad", "ubicacion", "costo", "categoria", "unidad", "nota"];
            for (int i = 0; i < columns.Length; i++)
                worksheet.Cell(1, i + 1).Value = columns[i];

            worksheet.Row(1).Style.Font.Bold = true;
            worksheet.Row(1).Style.Fill.BackgroundColor = XLColor.FromHtml("#4F81BD");
            worksheet.Row(1).Style.Font.FontColor = XLColor.White;
            worksheet.SheetView.FreezeRows(1);

            worksheet.Cell(2, 1).SetValue("1001");
            worksheet.Cell(2, 2).SetValue("Ejemplo de producto");
            worksheet.Cell(2, 3).SetValue(10);
            worksheet.Cell(2, 4).SetValue("Bodega A");
            worksheet.Cell(2, 5).SetValue(12.50);
            worksheet.Cell(2, 6).SetValue("General");
            worksheet.Cell(2, 7).SetValue("UN");
            worksheet.Cell(2, 8).SetValue("Ejemplo");

            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        public Task<string> GetNextNumAsync()
        {
            return Task.FromResult(BuildNextNum());
        }

        private string BuildNextNum()
        {
            var last = context.CargasIniciales
                .OrderByDescending(i => i.Id)
                .FirstOrDefault();
            int next = 1;
            if (last != null && int.TryParse(last.Numero, out int parsed))
                next = parsed + 1;
            return next.ToString("D4");
        }

        private static string GetCell(IXLWorksheet worksheet, int row, int col)
        {
            if (col <= 0) return "";
            return worksheet.Cell(row, col).GetString().Trim();
        }

        private async Task<HashSet<string>> ObtenerCatalogoCodigosAsync()
        {
            var productosExistentes = await context.Productos
                .Select(p => new { p.Codebar, p.Product_Code })
                .ToListAsync();

            var codigos = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var prod in productosExistentes)
            {
                if (!string.IsNullOrWhiteSpace(prod.Codebar))
                    codigos.Add(prod.Codebar.Trim());
                if (!string.IsNullOrWhiteSpace(prod.Product_Code))
                    codigos.Add(prod.Product_Code.Trim());
            }
            return codigos;
        }

        private async Task<Shared.Dtos.Product?> ResolverProductoAsync(string codigo)
        {
            if (string.IsNullOrWhiteSpace(codigo)) return null;

            var normalized = codigo.Trim();
            return await context.Productos.FirstOrDefaultAsync(p =>
                (p.Codebar != null && p.Codebar.ToLower() == normalized.ToLower()) ||
                (p.Product_Code != null && p.Product_Code.ToLower() == normalized.ToLower()));
        }

        private static bool TryParseDecimal(string raw, out decimal value)
        {
            if (decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out value))
                return true;
            return decimal.TryParse(raw, NumberStyles.Any, CultureInfo.CurrentCulture, out value);
        }
    }
}