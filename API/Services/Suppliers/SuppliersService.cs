using API.Data;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Shared.Dtos;

namespace API.Services.Suppliers
{
    public class SuppliersService(ApplicationDbContext context, ILogger<SuppliersService> logger) : ISuppliersService
    {
        private readonly ApplicationDbContext context = context;
        private readonly ILogger<SuppliersService> _logger = logger;

        private async Task EnsureSuppliersTableAsync()
        {
            await context.Database.ExecuteSqlRawAsync(@"
IF OBJECT_ID(N'[Suppliers]', N'U') IS NULL
BEGIN
    CREATE TABLE [Suppliers]
    (
        [SupplierId] int IDENTITY(1,1) NOT NULL CONSTRAINT [PK_Suppliers] PRIMARY KEY,
        [SupplierCode] nvarchar(20) NOT NULL CONSTRAINT [DF_Suppliers_SupplierCode] DEFAULT '',
        [SupplierName] nvarchar(150) NOT NULL,
        [Ruc] nvarchar(20) NOT NULL CONSTRAINT [DF_Suppliers_Ruc] DEFAULT '',
        [ContactName] nvarchar(100) NOT NULL CONSTRAINT [DF_Suppliers_ContactName] DEFAULT '',
        [Phone] nvarchar(30) NOT NULL CONSTRAINT [DF_Suppliers_Phone] DEFAULT '',
        [Email] nvarchar(100) NOT NULL CONSTRAINT [DF_Suppliers_Email] DEFAULT '',
        [Address] nvarchar(250) NOT NULL CONSTRAINT [DF_Suppliers_Address] DEFAULT '',
        [City] nvarchar(100) NOT NULL CONSTRAINT [DF_Suppliers_City] DEFAULT '',
        [Country] nvarchar(100) NOT NULL CONSTRAINT [DF_Suppliers_Country] DEFAULT '',
        [Website] nvarchar(150) NOT NULL CONSTRAINT [DF_Suppliers_Website] DEFAULT '',
        [IsActive] bit NOT NULL CONSTRAINT [DF_Suppliers_IsActive] DEFAULT 1,
        [CreatedAt] datetime NOT NULL CONSTRAINT [DF_Suppliers_CreatedAt] DEFAULT GETDATE(),
        [UpdatedAt] datetime NOT NULL CONSTRAINT [DF_Suppliers_UpdatedAt] DEFAULT GETDATE()
    );

    CREATE UNIQUE NONCLUSTERED INDEX [IX_Suppliers_SupplierCode] ON [Suppliers]([SupplierCode]);
END");
        }

        private static string FormatSupplierCode(int number)
        {
            return $"P{number:D6}";
        }

        private async Task<int> GetMaxSupplierCodeNumberAsync()
        {
            var codes = await context.Suppliers
                .Where(s => s.SupplierCode != null && s.SupplierCode.StartsWith("P"))
                .Select(s => s.SupplierCode)
                .ToListAsync();

            var max = 0;
            foreach (var code in codes)
            {
                if (code.Length == 7 && int.TryParse(code.AsSpan(1), out var number) && number > max)
                    max = number;
            }
            return max;
        }

        public async Task<string> GetNextNumAsync()
        {
            var max = await GetMaxSupplierCodeNumberAsync();
            return FormatSupplierCode(max + 1);
        }

        public async Task<List<Supplier>> GetSuppliersAsync()
        {
            await EnsureSuppliersTableAsync();
            return await context.Suppliers.OrderBy(s => s.SupplierName).ToListAsync();
        }

        public async Task<Supplier?> GetSupplierByIdAsync(int supplierId)
        {
            await EnsureSuppliersTableAsync();
            return await context.Suppliers.FirstOrDefaultAsync(s => s.SupplierId == supplierId);
        }

        public async Task<Supplier?> CreateSupplierAsync(Supplier supplier)
        {
            await EnsureSuppliersTableAsync();
            if (supplier == null || string.IsNullOrWhiteSpace(supplier.SupplierName)) return null;

            var name = supplier.SupplierName.Trim();
            var ruc = supplier.Ruc?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(name)) return null;

            if (!string.IsNullOrWhiteSpace(ruc) &&
                await context.Suppliers.AnyAsync(s => s.Ruc == ruc))
                return null;

            supplier.SupplierName = name;
            supplier.Ruc = ruc;
            supplier.ContactName = supplier.ContactName?.Trim() ?? string.Empty;
            supplier.Phone = supplier.Phone?.Trim() ?? string.Empty;
            supplier.Email = supplier.Email?.Trim() ?? string.Empty;
            supplier.Address = supplier.Address?.Trim() ?? string.Empty;
            supplier.City = supplier.City?.Trim() ?? string.Empty;
            supplier.Country = supplier.Country?.Trim() ?? string.Empty;
            supplier.Website = supplier.Website?.Trim() ?? string.Empty;
            supplier.CreatedAt = DateTime.Now;
            supplier.UpdatedAt = DateTime.Now;

            for (int attempt = 0; attempt < 3; attempt++)
            {
                supplier.SupplierCode = FormatSupplierCode(await GetMaxSupplierCodeNumberAsync() + 1);
                context.Suppliers.Add(supplier);
                try
                {
                    await context.SaveChangesAsync();
                    return supplier;
                }
                catch (DbUpdateException)
                {
                    context.Entry(supplier).State = EntityState.Detached;
                }
            }

            return null;
        }

        public async Task<Supplier?> UpdateSupplierAsync(int id, Supplier supplier)
        {
            await EnsureSuppliersTableAsync();
            if (supplier == null || string.IsNullOrWhiteSpace(supplier.SupplierName)) return null;

            var existing = await context.Suppliers.FindAsync(id);
            if (existing == null) return null;

            var name = supplier.SupplierName.Trim();
            var ruc = supplier.Ruc?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(name)) return null;

            if (!string.IsNullOrWhiteSpace(ruc) &&
                await context.Suppliers.AnyAsync(s => s.SupplierId != id && s.Ruc == ruc))
                return null;

            existing.SupplierName = name;
            existing.Ruc = ruc;
            existing.ContactName = supplier.ContactName?.Trim() ?? string.Empty;
            existing.Phone = supplier.Phone?.Trim() ?? string.Empty;
            existing.Email = supplier.Email?.Trim() ?? string.Empty;
            existing.Address = supplier.Address?.Trim() ?? string.Empty;
            existing.City = supplier.City?.Trim() ?? string.Empty;
            existing.Country = supplier.Country?.Trim() ?? string.Empty;
            existing.Website = supplier.Website?.Trim() ?? string.Empty;
            existing.IsActive = supplier.IsActive;
            existing.UpdatedAt = DateTime.Now;
            await context.SaveChangesAsync();
            return existing;
        }

        public async Task<bool> DeleteSupplierAsync(int id)
        {
            await EnsureSuppliersTableAsync();
            var supplier = await context.Suppliers.FindAsync(id);
            if (supplier == null) return false;

            context.Suppliers.Remove(supplier);
            await context.SaveChangesAsync();
            return true;
        }

        public async Task<SupplierImportResult> ImportFromExcelAsync(Stream excelStream)
        {
            var result = new SupplierImportResult();
            var rows = new List<(int Row, Supplier Supplier)>();
            var fileKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            using var workbook = new XLWorkbook(excelStream);
            var worksheet = workbook.Worksheets.FirstOrDefault();
            if (worksheet == null)
            {
                result.Success = false;
                result.Errors.Add(new SupplierImportError { Row = 0, Message = "El archivo no contiene hojas de cálculo." });
                return result;
            }

            var lastRow = worksheet.LastRowUsed();
            if (lastRow == null)
            {
                result.Success = false;
                result.Errors.Add(new SupplierImportError { Row = 0, Message = "El archivo está vacío." });
                return result;
            }

            var columns = ParseHeaderRow(worksheet);
            if (columns.SupplierName == null)
            {
                result.Success = false;
                result.Errors.Add(new SupplierImportError { Row = 0, Message = "No se encontró la columna obligatoria 'nombre'. Usa la plantilla de proveedores." });
                return result;
            }

            var (firstDataRow, lastDataRow) = GetDataRange(worksheet, columns.SupplierName.Value, lastRow.RowNumber());

            for (int i = firstDataRow; i <= lastDataRow; i++)
            {
                var supplierName = GetCell(worksheet, i, columns.SupplierName!.Value);
                if (string.IsNullOrWhiteSpace(supplierName))
                    continue;

                var ruc = columns.Ruc.HasValue ? GetCell(worksheet, i, columns.Ruc.Value) : "";
                var contactName = columns.ContactName.HasValue ? GetCell(worksheet, i, columns.ContactName.Value) : "";
                var phone = columns.Phone.HasValue ? GetCell(worksheet, i, columns.Phone.Value) : "";
                var email = columns.Email.HasValue ? GetCell(worksheet, i, columns.Email.Value) : "";
                var address = columns.Address.HasValue ? GetCell(worksheet, i, columns.Address.Value) : "";
                var city = columns.City.HasValue ? GetCell(worksheet, i, columns.City.Value) : "";
                var country = columns.Country.HasValue ? GetCell(worksheet, i, columns.Country.Value) : "";
                var website = columns.Website.HasValue ? GetCell(worksheet, i, columns.Website.Value) : "";

                if (!string.IsNullOrWhiteSpace(ruc) && !fileKeys.Add(ruc))
                {
                    result.Skipped++;
                    result.Errors.Add(new SupplierImportError { Row = i, Message = $"El RUC '{ruc}' está duplicado en el archivo." });
                    continue;
                }

                rows.Add((i, new Supplier
                {
                    SupplierName = supplierName,
                    Ruc = ruc,
                    ContactName = contactName,
                    Phone = phone,
                    Email = email,
                    Address = address,
                    City = city,
                    Country = country,
                    Website = website
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
                var existing = await context.Suppliers.ToListAsync();
                var byRuc = existing
                    .Where(s => !string.IsNullOrWhiteSpace(s.Ruc))
                    .ToDictionary(s => s.Ruc, s => s, StringComparer.OrdinalIgnoreCase);

                var nextCodeNumber = 0;
                foreach (var s in existing)
                {
                    var code = s.SupplierCode;
                    if (!string.IsNullOrWhiteSpace(code) && code.Length == 7 &&
                        code.StartsWith("P") && int.TryParse(code.AsSpan(1), out var n) && n > nextCodeNumber)
                        nextCodeNumber = n;
                }

                foreach (var (_, supplier) in rows)
                {
                    if (!string.IsNullOrWhiteSpace(supplier.Ruc) &&
                        byRuc.TryGetValue(supplier.Ruc, out var existingSupplier))
                    {
                        existingSupplier.SupplierName = supplier.SupplierName;
                        existingSupplier.ContactName = supplier.ContactName;
                        existingSupplier.Phone = supplier.Phone;
                        existingSupplier.Email = supplier.Email;
                        existingSupplier.Address = supplier.Address;
                        existingSupplier.City = supplier.City;
                        existingSupplier.Country = supplier.Country;
                        existingSupplier.Website = supplier.Website;
                        existingSupplier.UpdatedAt = DateTime.Now;
                        result.Updated++;
                    }
                    else
                    {
                        supplier.SupplierCode = FormatSupplierCode(++nextCodeNumber);
                        supplier.CreatedAt = DateTime.Now;
                        supplier.UpdatedAt = DateTime.Now;
                        context.Suppliers.Add(supplier);
                        result.Inserted++;
                    }
                }

                await context.SaveChangesAsync();
                await transaction.CommitAsync();
                result.Success = true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error al importar proveedores: se revirtió toda la operación.");
                result.Success = false;
                result.Errors.Add(new SupplierImportError { Row = 0, Message = $"Error interno al procesar el archivo: {ex.Message}" });
            }

            return result;
        }

        private enum ImportColumn
        {
            SupplierName, Ruc, ContactName, Phone, Email, Address, City, Country, Website
        }

        private sealed class ImportColumns
        {
            public int? SupplierName { get; set; }
            public int? Ruc { get; set; }
            public int? ContactName { get; set; }
            public int? Phone { get; set; }
            public int? Email { get; set; }
            public int? Address { get; set; }
            public int? City { get; set; }
            public int? Country { get; set; }
            public int? Website { get; set; }
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
            [ImportColumn.SupplierName] = ["nombre", "proveedor", "razonsocial", "supplier", "name"],
            [ImportColumn.Ruc] = ["ruc", "registrofiscal", "taxid", "ncf"],
            [ImportColumn.ContactName] = ["contacto", "contactname", "personacontacto"],
            [ImportColumn.Phone] = ["telefono", "phone", "teléfono", "celular"],
            [ImportColumn.Email] = ["email", "correo", "correoelectronico", "mail"],
            [ImportColumn.Address] = ["direccion", "address", "dirección"],
            [ImportColumn.City] = ["ciudad", "city", "municipio"],
            [ImportColumn.Country] = ["pais", "country", "país"],
            [ImportColumn.Website] = ["web", "website", "sitio", "url"],
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
                        case ImportColumn.SupplierName when parsed.SupplierName is null: parsed.SupplierName = col; break;
                        case ImportColumn.Ruc when parsed.Ruc is null: parsed.Ruc = col; break;
                        case ImportColumn.ContactName when parsed.ContactName is null: parsed.ContactName = col; break;
                        case ImportColumn.Phone when parsed.Phone is null: parsed.Phone = col; break;
                        case ImportColumn.Email when parsed.Email is null: parsed.Email = col; break;
                        case ImportColumn.Address when parsed.Address is null: parsed.Address = col; break;
                        case ImportColumn.City when parsed.City is null: parsed.City = col; break;
                        case ImportColumn.Country when parsed.Country is null: parsed.Country = col; break;
                        case ImportColumn.Website when parsed.Website is null: parsed.Website = col; break;
                    }
                }
            }

            return parsed;
        }

        private static (int First, int Last) GetDataRange(IXLWorksheet worksheet, int supplierNameColumn, int lastRow)
        {
            for (int i = 2; i <= lastRow; i++)
            {
                if (!string.IsNullOrWhiteSpace(worksheet.Cell(i, supplierNameColumn).GetString()))
                    return (i, lastRow);
            }
            return (2, 2);
        }

        private static string GetCell(IXLWorksheet worksheet, int row, int col)
        {
            return worksheet.Cell(row, col).GetString().Trim();
        }
    }
}
