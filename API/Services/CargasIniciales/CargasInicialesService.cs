using API.Data;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
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

        public async Task<Inicial> CreateAsync(Inicial inicial)
        {
            if (string.IsNullOrWhiteSpace(inicial.Numero))
                inicial.Numero = BuildNextNum();
            inicial.FechaCreacion = DateTime.Now;

            context.CargasIniciales.Add(inicial);
            await context.SaveChangesAsync();
            return inicial;
        }

        public async Task<Inicial?> UpdateAsync(int id, Inicial inicial)
        {
            var existing = await context.CargasIniciales
                .Include(i => i.Detalles)
                .FirstOrDefaultAsync(i => i.Id == id);
            if (existing == null) return null;

            existing.Numero = inicial.Numero;
            existing.Comentario = inicial.Comentario;
            existing.FechaCreacion = inicial.FechaCreacion;

            context.CargasInicialesDetalles.RemoveRange(existing.Detalles);
            await context.SaveChangesAsync();

            foreach (var detalle in inicial.Detalles)
            {
                detalle.Id = 0;
                detalle.InicialId = id;
                context.CargasInicialesDetalles.Add(detalle);
            }

            await context.SaveChangesAsync();
            return await GetByIdAsync(id);
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

            var headers = new Dictionary<string, int>();
            var headerRow = worksheet.Row(1);
            var lastCol = headerRow.LastCellUsed()?.Address.ColumnNumber ?? 0;
            for (int col = 1; col <= lastCol; col++)
            {
                var name = NormalizeHeader(headerRow.Cell(col).GetString());
                if (!string.IsNullOrEmpty(name))
                    headers[name] = col;
            }

            if (!headers.TryGetValue("productcode", out _))
            {
                result.Success = false;
                result.Errors.Add(new RowError { Row = 0, Message = "No se encontró la columna obligatoria 'product_code'. Usa la plantilla." });
                return result;
            }

            var lastDataRow = worksheet.LastRowUsed()?.RowNumber() ?? 1;
            for (int row = 2; row <= lastDataRow; row++)
            {
                var productCode = GetCell(worksheet, row, headers.GetValueOrDefault("productcode", -1));
                var productName = GetCell(worksheet, row, headers.GetValueOrDefault("productname", -1));
                var cantidad = GetCell(worksheet, row, headers.GetValueOrDefault("cantidad", -1));
                var ubicacion = GetCell(worksheet, row, headers.GetValueOrDefault("ubicacion", -1));
                var costo = GetCell(worksheet, row, headers.GetValueOrDefault("costo", -1));
                var categoria = GetCell(worksheet, row, headers.GetValueOrDefault("categoria", -1));
                var unidad = GetCell(worksheet, row, headers.GetValueOrDefault("unidad", -1));
                var nota = GetCell(worksheet, row, headers.GetValueOrDefault("nota", -1));

                if (string.IsNullOrWhiteSpace(productCode) && string.IsNullOrWhiteSpace(productName) &&
                    string.IsNullOrWhiteSpace(cantidad) && string.IsNullOrWhiteSpace(ubicacion))
                    continue;

                if (string.IsNullOrWhiteSpace(productCode))
                {
                    result.Skipped++;
                    result.Errors.Add(new RowError { Row = row, Message = "El código de producto es obligatorio." });
                    continue;
                }

                if (!int.TryParse(cantidad, NumberStyles.Integer, CultureInfo.InvariantCulture, out var cantidadParsed))
                    cantidadParsed = 0;

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

            var inicial = new Inicial
            {
                Numero = BuildNextNum(),
                FechaCreacion = DateTime.Now,
                Comentario = "Carga inicial importada desde Excel",
                Detalles = detalles
            };

            context.CargasIniciales.Add(inicial);
            await context.SaveChangesAsync();

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

        private string BuildNextNum()
        {
            var last = context.CargasIniciales
                .OrderByDescending(i => i.Id)
                .FirstOrDefault();
            int next = 1;
            if (last != null && int.TryParse(last.Numero, out int parsed))
                next = parsed + 1;
            return next.ToString("D6");
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

        private static string GetCell(IXLWorksheet worksheet, int row, int col)
        {
            if (col <= 0) return "";
            return worksheet.Cell(row, col).GetString().Trim();
        }

        private static bool TryParseDecimal(string raw, out decimal value)
        {
            if (decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out value))
                return true;
            return decimal.TryParse(raw, NumberStyles.Any, CultureInfo.CurrentCulture, out value);
        }
    }
}