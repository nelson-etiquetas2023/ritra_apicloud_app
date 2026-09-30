using Microsoft.EntityFrameworkCore;
using ScanProMovil.Data;
using ScanProMovil.Data.Entities;
using ScanProMovil.Entities;
using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;

namespace ScanProMovil.Services.StockInicial
{
    public class StockInitService : IStockInitService
    {
        private readonly AppDbContext _context;
        private readonly IHttpClientFactory _httpFactory;
        private static readonly JsonSerializerOptions jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true
        };

        public StockInitService(AppDbContext context, IHttpClientFactory httpFactory)
        {
            _context = context;
            _httpFactory = httpFactory;
        }

        public async Task<List<StockInit>> GetAllAsync()
        {
            return await _context.StockInits
                .AsNoTracking()
                .Include(d => d.Items)
                .OrderByDescending(d => d.Fecha)
                .ThenByDescending(d => d.Numero)
                .ToListAsync();
        }

        public async Task<StockInit?> GetByIdAsync(string numero)
        {
            return await _context.StockInits
                .AsNoTracking()
                .Include(d => d.Items)
                .FirstOrDefaultAsync(d => d.Numero == numero);
        }

        public async Task<List<StockInit>> SearchAsync(string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
                return await GetAllAsync();

            var term = searchText.Trim().ToLower();
            return await _context.StockInits
                .AsNoTracking()
                .Include(d => d.Items)
                .Where(d => d.Numero.ToLower().Contains(term)
                         || (d.Description != null && d.Description.ToLower().Contains(term)))
                .OrderByDescending(d => d.Fecha)
                .ThenByDescending(d => d.Numero)
                .ToListAsync();
        }

        public async Task<bool> CreateAsync(StockInit doc)
        {
            try
            {
                using var tx = await _context.Database.BeginTransactionAsync();
                _context.StockInits.Add(doc);
                await _context.SaveChangesAsync();
                await tx.CommitAsync();
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error al crear StockInit: " + ex.Message);
                return false;
            }
        }

        public async Task<bool> UpdateAsync(StockInit doc)
        {
            try
            {
                using var tx = await _context.Database.BeginTransactionAsync();

                var existente = await _context.StockInits
                    .Include(d => d.Items)
                    .FirstOrDefaultAsync(d => d.Numero == doc.Numero);
                if (existente is null) return false;

                existente.Fecha = doc.Fecha;
                existente.Description = doc.Description;
                existente.Status = doc.Status;

                _context.StockItems.RemoveRange(existente.Items);
                await _context.SaveChangesAsync();

                foreach (var item in doc.Items)
                {
                    item.Id = 0;
                    item.Numero = doc.Numero;
                }

                existente.Items.Clear();
                foreach (var item in doc.Items)
                    existente.Items.Add(item);
                await _context.SaveChangesAsync();

                await tx.CommitAsync();
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error al actualizar StockInit: " + ex.Message);
                return false;
            }
        }

        public async Task<bool> DeleteAsync(string numero)
        {
            try
            {
                var doc = await _context.StockInits
                    .FirstOrDefaultAsync(d => d.Numero == numero);
                if (doc is null) return false;

                _context.StockInits.Remove(doc);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error al eliminar StockInit: " + ex.Message);
                return false;
            }
        }

        public async Task<string> GetNextNumberAsync()
        {
            // Identidad local del conteo. No es el correlativo oficial: el
            // servidor asigna el FIS-#### al crear el documento y se guarda en
            // NumeroServidor. Asi dos dispositivos pueden contar sin conexión
            // sin colisionar en el índice único del servidor.
            const string tipo = "TMP";

            using var tx = await _context.Database.BeginTransactionAsync();

            var consecutivo = await _context.ConsecutivosStockInit
                .FirstOrDefaultAsync(c => c.Tipo_Documento == tipo);

            if (consecutivo is null)
            {
                // Primera vez: se toma como base el mayor correlativo ya existente
                // para no repetir un número que ya se usó en este dispositivo.
                var max = await _context.StockInits
                    .AsNoTracking()
                    .Where(d => d.Numero.StartsWith(tipo + "-"))
                    .Select(d => d.Numero)
                    .ToListAsync();

                var ultimo = 0;
                foreach (var numero in max)
                {
                    if (int.TryParse(numero.AsSpan(tipo.Length + 1), out var value))
                        ultimo = Math.Max(ultimo, value);
                }

                consecutivo = new ConsecutivoStockInit { Tipo_Documento = tipo, UltimoNumero = ultimo };
                _context.ConsecutivosStockInit.Add(consecutivo);
            }

            consecutivo.UltimoNumero += 1;
            await _context.SaveChangesAsync();
            await tx.CommitAsync();

            return $"{tipo}-{consecutivo.UltimoNumero:D4}";
        }

        public async Task<SincroStockInitResult> SincronizarAsync(string numero)
        {
            var result = new SincroStockInitResult();
            try
            {
                var doc = await _context.StockInits
                    .Include(d => d.Items)
                    .FirstOrDefaultAsync(d => d.Numero == numero);

                if (doc is null)
                {
                    result.Message = $"El documento {numero} no fue encontrado.";
                    return result;
                }

                if (doc.Items.Count == 0)
                {
                    result.Message = "El documento no tiene ítems para sincronizar.";
                    return result;
                }

                var pendientes = doc.Items.Where(i => !i.Enviado).ToList();
                if (pendientes.Count == 0)
                {
                    result.Success = true;
                    result.Message = "Todos los ítems del documento ya fueron sincronizados.";
                    return result;
                }

                var payload = new InventarioFisicoDto
                {
                    // En la creación el servidor asigna el correlativo e ignora
                    // este valor; en la actualización ya es el FIS-#### oficial.
                    Numero = doc.NumeroServidor ?? doc.Numero,
                    FechaCreacion = doc.Fecha,
                    Comentario = doc.Description,
                    Status = 1,
                    Detalles = pendientes.Select(i => new InventarioFisicoDetalleDto
                    {
                        ProductCode = i.Product_Code,
                        ProductName = i.Product_Name,
                        CantidadFisica = i.Cantidad,
                        Ubicacion = i.Ubicacion ?? "",
                        Costo = (decimal)i.Costo,
                        Nota = i.Nota ?? "",
                        Procesado = false
                    }).ToList()
                };

                var client = _httpFactory.CreateClient("scanpro");

                // Un conteo ya sincronizado se actualiza por su correlativo oficial.
                // Uno que nunca se subio se crea: el servidor asigna el FIS-####.
                InventarioFisicoDto? existente = null;
                if (!string.IsNullOrWhiteSpace(doc.NumeroServidor))
                {
                    var existentes = await client.GetFromJsonAsync<List<InventarioFisicoDto>>("api/InventarioFisico/get", jsonOptions);
                    existente = existentes?.FirstOrDefault(x =>
                        string.Equals(x.Numero, doc.NumeroServidor, StringComparison.OrdinalIgnoreCase));
                }

                if (existente is not null && existente.Status == 4)
                {
                    result.Message = $"El conteo {doc.NumeroServidor} ya fue aplicado al inventario desde la web. " +
                                     "Cree un nuevo conteo si necesita registrar cambios.";
                    return result;
                }

                HttpResponseMessage response;
                if (existente is not null)
                    response = await client.PutAsJsonAsync($"api/InventarioFisico/update/{existente.Id}", payload, jsonOptions);
                else
                    response = await client.PostAsJsonAsync("api/InventarioFisico/create", payload, jsonOptions);

                var body = await response.Content.ReadAsStringAsync();
                if (!response.IsSuccessStatusCode)
                {
                    result.Message = $"El servidor respondió HTTP {(int)response.StatusCode}: {body}";
                    return result;
                }

                var serverResult = JsonSerializer.Deserialize<FiscalSaveResultDto>(body, jsonOptions);
                if (serverResult is null || !serverResult.Success)
                {
                    result.Message = serverResult?.Message ?? "El servidor no confirmó la sincronización.";
                    return result;
                }

                // El servidor ignora el número local y asigna el correlativo oficial.
                var numeroAsignado = serverResult.Data?.Numero;
                if (existente is null && !string.IsNullOrWhiteSpace(numeroAsignado))
                    doc.NumeroServidor = numeroAsignado;

                foreach (var item in pendientes)
                {
                    item.Enviado = true;
                }

                doc.Status = doc.Items.All(i => i.Enviado) ? "Sincronizado" : "Actualizado";
                await _context.SaveChangesAsync();

                var restantes = doc.Items.Count(i => !i.Enviado);
                result.Success = true;
                result.Message = restantes == 0
                    ? $"El documento {doc.NumeroVisible} fue sincronizado correctamente ({pendientes.Count} ítems)."
                    : $"Se sincronizaron {pendientes.Count} ítems. Quedan {restantes} pendientes.";
                return result;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error al sincronizar StockInit: " + ex.Message);
                result.Message = "Error al sincronizar: " + ex.Message;
                return result;
            }
        }
    }

    public class InventarioFisicoDto
    {
        public int Id { get; set; }
        public string Numero { get; set; } = "";
        public DateTime FechaCreacion { get; set; }
        public string DocumentoTeorico { get; set; } = "";
        public string Comentario { get; set; } = "";
        public int Status { get; set; }
        public ICollection<InventarioFisicoDetalleDto> Detalles { get; set; } = new List<InventarioFisicoDetalleDto>();
    }

    public class InventarioFisicoDetalleDto
    {
        public string ProductCode { get; set; } = "";
        public string ProductName { get; set; } = "";
        public double CantidadFisica { get; set; }
        public string Ubicacion { get; set; } = "";
        public decimal Costo { get; set; }
        public string Categoria { get; set; } = "";
        public string Unidad { get; set; } = "";
        public string Nota { get; set; } = "";
        public bool Procesado { get; set; }
    }

    public class FiscalSaveResultDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public InventarioFisicoDto? Data { get; set; }
    }
}