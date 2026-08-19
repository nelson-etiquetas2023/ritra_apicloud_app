using API.Data;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared.Dtos;

namespace API.Services.Customers
{
    public class CustomersService(ApplicationDbContext context, ILogger<CustomersService> logger) : ICustomersService
    {
        private readonly ApplicationDbContext context = context;
        private readonly ILogger<CustomersService> _logger = logger;

        private async Task EnsureCustomersTableAsync()
        {
            await context.Database.ExecuteSqlRawAsync(@"
IF OBJECT_ID(N'[Customers]', N'U') IS NULL
BEGIN
    CREATE TABLE [Customers]
    (
        [customer_id] int IDENTITY(1,1) NOT NULL CONSTRAINT [PK_Customers] PRIMARY KEY,
        [CustomerCode] nvarchar(20) NOT NULL CONSTRAINT [DF_Customers_CustomerCode] DEFAULT '',
        [CustomerName] nvarchar(150) NOT NULL CONSTRAINT [DF_Customers_CustomerName] DEFAULT '',
        [Direccion] nvarchar(max) NOT NULL,
        [Registro_Fiscal] nvarchar(max) NOT NULL,
        [Telefono] nvarchar(max) NOT NULL,
        [Correo] nvarchar(max) NOT NULL,
        [Email] nvarchar(max) NOT NULL
    );

    CREATE UNIQUE NONCLUSTERED INDEX [IX_Customers_CustomerCode] ON [Customers]([CustomerCode]);
END");
        }

        private static string FormatCustomerCode(int number)
        {
            return $"C{number:D6}";
        }

        private async Task<int> GetMaxCustomerCodeNumberAsync()
        {
            var codes = await context.Customers
                .Where(c => c.CustomerCode != null && c.CustomerCode.StartsWith("C"))
                .Select(c => c.CustomerCode)
                .ToListAsync();

            var max = 0;
            foreach (var code in codes)
            {
                if (code.Length == 7 && int.TryParse(code.AsSpan(1), out var number) && number > max)
                    max = number;
            }
            return max;
        }

        public async Task<List<Customer>> GetCustomersAsync()
        {
            await EnsureCustomersTableAsync();
            return await context.Customers.OrderBy(c => c.CustomerName).ToListAsync();
        }

        public async Task<Customer?> GetCustomerByIdAsync(int customerId)
        {
            await EnsureCustomersTableAsync();
            return await context.Customers.FirstOrDefaultAsync(c => c.customer_id == customerId);
        }

        public async Task<Customer?> CreateCustomerAsync(Customer customer)
        {
            await EnsureCustomersTableAsync();
            if (customer == null || string.IsNullOrWhiteSpace(customer.CustomerName)) return null;

            var name = customer.CustomerName.Trim();
            if (string.IsNullOrWhiteSpace(name)) return null;

            customer.CustomerName = name;
            customer.Direccion = customer.Direccion?.Trim() ?? string.Empty;
            customer.Registro_Fiscal = customer.Registro_Fiscal?.Trim() ?? string.Empty;
            customer.Telefono = customer.Telefono?.Trim() ?? string.Empty;
            customer.Correo = customer.Correo?.Trim() ?? string.Empty;
            customer.Email = customer.Email?.Trim() ?? string.Empty;

            for (int attempt = 0; attempt < 3; attempt++)
            {
                customer.CustomerCode = FormatCustomerCode(await GetMaxCustomerCodeNumberAsync() + 1);
                context.Customers.Add(customer);
                try
                {
                    await context.SaveChangesAsync();
                    return customer;
                }
                catch (DbUpdateException)
                {
                    context.Entry(customer).State = EntityState.Detached;
                }
            }

            return null;
        }

        public async Task<Customer?> UpdateCustomerAsync(int customerId, Customer customer)
        {
            await EnsureCustomersTableAsync();
            if (customer == null || string.IsNullOrWhiteSpace(customer.CustomerName)) return null;

            var existing = await context.Customers.FirstOrDefaultAsync(c => c.customer_id == customerId);
            if (existing == null) return null;

            existing.CustomerName = customer.CustomerName.Trim();
            existing.Direccion = customer.Direccion?.Trim() ?? string.Empty;
            existing.Registro_Fiscal = customer.Registro_Fiscal?.Trim() ?? string.Empty;
            existing.Telefono = customer.Telefono?.Trim() ?? string.Empty;
            existing.Correo = customer.Correo?.Trim() ?? string.Empty;
            existing.Email = customer.Email?.Trim() ?? string.Empty;

            await context.SaveChangesAsync();
            return existing;
        }

        public async Task<bool> DeleteCustomerAsync(int customerId)
        {
            await EnsureCustomersTableAsync();
            var customer = await context.Customers.FirstOrDefaultAsync(c => c.customer_id == customerId);
            if (customer == null) return false;

            context.Customers.Remove(customer);
            await context.SaveChangesAsync();
            return true;
        }

        public async Task<CustomerImportResult> ImportFromExcelAsync(Stream excelStream)
        {
            var result = new CustomerImportResult();
            var rows = new List<(int Row, Customer Customer)>();
            var fileKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            using var workbook = new XLWorkbook(excelStream);
            var worksheet = workbook.Worksheets.FirstOrDefault();
            if (worksheet == null)
            {
                result.Success = false;
                result.Errors.Add(new CustomerImportError { Row = 0, Message = "El archivo no contiene hojas de cálculo." });
                return result;
            }

            var lastRow = worksheet.LastRowUsed();
            if (lastRow == null)
            {
                result.Success = false;
                result.Errors.Add(new CustomerImportError { Row = 0, Message = "El archivo está vacío." });
                return result;
            }

            var columns = ParseHeaderRow(worksheet);
            if (columns.CustomerName == null)
            {
                result.Success = false;
                result.Errors.Add(new CustomerImportError { Row = 0, Message = "No se encontró la columna obligatoria 'nombre'. Usa la plantilla de clientes." });
                return result;
            }

            var (firstDataRow, lastDataRow) = GetDataRange(worksheet, columns.CustomerName.Value, lastRow.RowNumber());

            for (int i = firstDataRow; i <= lastDataRow; i++)
            {
                var customerName = GetCell(worksheet, i, columns.CustomerName!.Value);
                if (string.IsNullOrWhiteSpace(customerName))
                    continue;

                var direccion = columns.Direccion.HasValue ? GetCell(worksheet, i, columns.Direccion.Value) : "";
                var registroFiscal = columns.RegistroFiscal.HasValue ? GetCell(worksheet, i, columns.RegistroFiscal.Value) : "";
                var telefono = columns.Telefono.HasValue ? GetCell(worksheet, i, columns.Telefono.Value) : "";
                var correo = columns.Correo.HasValue ? GetCell(worksheet, i, columns.Correo.Value) : "";
                var email = columns.Email.HasValue ? GetCell(worksheet, i, columns.Email.Value) : "";

                if (!string.IsNullOrWhiteSpace(registroFiscal) && !fileKeys.Add(registroFiscal))
                {
                    result.Skipped++;
                    result.Errors.Add(new CustomerImportError { Row = i, Message = $"El registro fiscal '{registroFiscal}' está duplicado en el archivo." });
                    continue;
                }

                rows.Add((i, new Customer
                {
                    CustomerName = customerName,
                    Direccion = direccion,
                    Registro_Fiscal = registroFiscal,
                    Telefono = telefono,
                    Correo = correo,
                    Email = email
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
                var existing = await context.Customers.ToListAsync();
                var byRegistro = existing
                    .Where(c => !string.IsNullOrWhiteSpace(c.Registro_Fiscal))
                    .ToDictionary(c => c.Registro_Fiscal, c => c, StringComparer.OrdinalIgnoreCase);

                var nextCodeNumber = 0;
                foreach (var c in existing)
                {
                    var code = c.CustomerCode;
                    if (!string.IsNullOrWhiteSpace(code) && code.Length == 7 &&
                        code.StartsWith("C") && int.TryParse(code.AsSpan(1), out var n) && n > nextCodeNumber)
                        nextCodeNumber = n;
                }

                foreach (var (_, customer) in rows)
                {
                    if (!string.IsNullOrWhiteSpace(customer.Registro_Fiscal) &&
                        byRegistro.TryGetValue(customer.Registro_Fiscal, out var existingCustomer))
                    {
                        existingCustomer.CustomerName = customer.CustomerName;
                        existingCustomer.Direccion = customer.Direccion;
                        existingCustomer.Telefono = customer.Telefono;
                        existingCustomer.Correo = customer.Correo;
                        existingCustomer.Email = customer.Email;
                        result.Updated++;
                    }
                    else
                    {
                        customer.CustomerCode = FormatCustomerCode(++nextCodeNumber);
                        context.Customers.Add(customer);
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
                _logger.LogError(ex, "Error al importar clientes: se revirtió toda la operación.");
                result.Success = false;
                result.Errors.Add(new CustomerImportError { Row = 0, Message = $"Error interno al procesar el archivo: {ex.Message}" });
            }

            return result;
        }

        private enum ImportColumn
        {
            CustomerName, Direccion, RegistroFiscal, Telefono, Correo, Email
        }

        private sealed class ImportColumns
        {
            public int? CustomerName { get; set; }
            public int? Direccion { get; set; }
            public int? RegistroFiscal { get; set; }
            public int? Telefono { get; set; }
            public int? Correo { get; set; }
            public int? Email { get; set; }
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
            [ImportColumn.CustomerName] = ["nombre", "cliente", "name", "razonsocial"],
            [ImportColumn.Direccion] = ["direccion", "address", "dirección"],
            [ImportColumn.RegistroFiscal] = ["registrofiscal", "ruc", "registrofiscal", "ncf", "taxid", "tipoidentificacion"],
            [ImportColumn.Telefono] = ["telefono", "phone", "teléfono", "celular"],
            [ImportColumn.Correo] = ["correo", "email", "correoelectronico", "mail"],
            [ImportColumn.Email] = ["email", "correo", "correoelectronico", "mail"],
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
                        case ImportColumn.CustomerName when parsed.CustomerName is null: parsed.CustomerName = col; break;
                        case ImportColumn.Direccion when parsed.Direccion is null: parsed.Direccion = col; break;
                        case ImportColumn.RegistroFiscal when parsed.RegistroFiscal is null: parsed.RegistroFiscal = col; break;
                        case ImportColumn.Telefono when parsed.Telefono is null: parsed.Telefono = col; break;
                        case ImportColumn.Correo when parsed.Correo is null: parsed.Correo = col; break;
                        case ImportColumn.Email when parsed.Email is null: parsed.Email = col; break;
                    }
                }
            }

            return parsed;
        }

        private static (int First, int Last) GetDataRange(IXLWorksheet worksheet, int customerNameColumn, int lastRow)
        {
            for (int i = 2; i <= lastRow; i++)
            {
                if (!string.IsNullOrWhiteSpace(worksheet.Cell(i, customerNameColumn).GetString()))
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