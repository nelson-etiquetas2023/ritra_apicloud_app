using Microsoft.EntityFrameworkCore;
using ScanProMovil.Data;
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
                existente.Document_Teorico = doc.Document_Teorico;
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
            const string prefix = "DIF-";
            var numeros = await _context.StockInits
                .AsNoTracking()
                .Select(d => d.Numero)
                .ToListAsync();

            var max = 0;
            foreach (var numero in numeros)
            {
                if (numero.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) &&
                    int.TryParse(numero.AsSpan(prefix.Length), out var value))
                {
                    max = Math.Max(max, value);
                }
            }

            return $"{prefix}{max + 1:D4}";
        }

        public async Task<bool> SeedDummyDataAsync()
        {
            var productos = new (string Code, string Name, double Costo, string Ubicacion)[]
            {
                ("P001", "Vive100 Original 850ml", 12.5, "A-01"),
                ("P002", "Vive100 Manzana 850ml", 12.5, "A-02"),
                ("P003", "Vive100 Cherry 850ml", 13.0, "B-01"),
                ("P004", "Gatorade Lima 1L", 18.0, "B-02"),
                ("P005", "Gatorade Naranja 1L", 18.0, "C-01"),
                ("P006", "Coca Cola 2L", 25.0, "C-02"),
                ("P007", "Pepsi 2L", 23.0, "D-01"),
                ("P008", "Agua Cielo 1L", 8.0, "D-02"),
            };

            var estados = new[] { "Iniciado", "Actualizado", "Sincronizado", "Cerrado" };
            var descripciones = new[]
            {
                "inventario de productos electrónicos bodega 1",
                "inventario de bebidas y alimentos bodega 1",
                "inventario de bebidas y alimentos bodega 2",
                "inventario de gaseosas y agua bodega 1",
                "inventario de energizantes y deportivas bodega 3",
                "inventario de licores y vinos bodega 2",
                "inventario de snacks y abarrotes bodega 1",
                "inventario de lácteos y refrigerados bodega 2",
                "inventario de productos de limpieza bodega 3",
                "inventario de panadería y repostería bodega 1",
            };

            if (!await _context.StockInits.AnyAsync())
            {
                for (var i = 1; i <= 10; i++)
                {
                    var numero = $"DIF-{i:D4}";
                    var rng = new Random(i * 7 + 3);
                    var itemCount = 2 + i % 4;

                    var doc = new StockInit
                    {
                        Numero = numero,
                        Fecha = DateTime.Today.AddDays(-(11 - i)),
                        Document_Teorico = $"DT-{i:D3}",
                        Description = descripciones[(i - 1) % descripciones.Length],
                        Status = estados[i % estados.Length],
                        Items = new List<StockItem>()
                    };

                    for (var j = 0; j < itemCount; j++)
                    {
                        var p = productos[rng.Next(productos.Length)];
                        var cantidad = rng.Next(5, 51);

                        doc.Items.Add(new StockItem
                        {
                            Numero = numero,
                            Product_Code = p.Code,
                            Product_Name = p.Name,
                            Cantidad = cantidad,
                            Costo = p.Costo,
                            TotalCosto = Math.Round(cantidad * p.Costo, 2),
                            Ubicacion = p.Ubicacion
                        });
                    }

                    _context.StockInits.Add(doc);
                }

                await _context.SaveChangesAsync();
                return true;
            }

            // Backfill: los documentos viejos (descripcion numerica o vacia) reciben un texto real
            // y el estado "Contado" se reemplaza por un estado valido.
            var docs = await _context.StockInits.OrderBy(d => d.Numero).ToListAsync();
            var changed = false;
            for (var i = 0; i < docs.Count; i++)
            {
                if (string.IsNullOrWhiteSpace(docs[i].Description) ||
                    double.TryParse(docs[i].Description, out _))
                {
                    docs[i].Description = descripciones[i % descripciones.Length];
                    changed = true;
                }

                if (string.Equals(docs[i].Status, "Contado", StringComparison.OrdinalIgnoreCase))
                {
                    docs[i].Status = "Actualizado";
                    changed = true;
                }
            }

            if (changed)
                await _context.SaveChangesAsync();

            return false;
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

                var payload = new InicialDto
                {
                    Numero = doc.Numero,
                    FechaCreacion = doc.Fecha,
                    Comentario = doc.Description,
                    Status = 1,
                    Detalles = pendientes.Select(i => new DetalleInicialDto
                    {
                        ProductCode = i.Product_Code,
                        ProductName = i.Product_Name,
                        Cantidad = (int)Math.Round(i.Cantidad),
                        CantidadFisica = (int)Math.Round(i.Cantidad),
                        Ubicacion = i.Ubicacion ?? "",
                        Costo = (decimal)i.Costo,
                        Nota = i.Nota ?? "",
                        Procesado = false
                    }).ToList()
                };

                var client = _httpFactory.CreateClient("scanpro");

                var existentes = await client.GetFromJsonAsync<List<InicialDto>>("api/cargasIniciales/get", jsonOptions);
                var existente = existentes?.FirstOrDefault(x => x.Numero == numero);

                HttpResponseMessage response;
                if (existente is not null)
                    response = await client.PutAsJsonAsync($"api/cargasIniciales/update/{existente.Id}", payload, jsonOptions);
                else
                    response = await client.PostAsJsonAsync("api/cargasIniciales/create", payload, jsonOptions);

                var body = await response.Content.ReadAsStringAsync();
                if (!response.IsSuccessStatusCode)
                {
                    result.Message = $"El servidor respondió HTTP {(int)response.StatusCode}: {body}";
                    return result;
                }

                var serverResult = JsonSerializer.Deserialize<CargaInicialSaveResultDto>(body, jsonOptions);
                if (serverResult is null || !serverResult.Success)
                {
                    result.Message = serverResult?.Message ?? "El servidor no confirmó la sincronización.";
                    return result;
                }

                foreach (var item in pendientes)
                {
                    item.Enviado = true;
                }

                doc.Status = doc.Items.All(i => i.Enviado) ? "Sincronizado" : "Actualizado";
                await _context.SaveChangesAsync();

                var restantes = doc.Items.Count(i => !i.Enviado);
                result.Success = true;
                result.Message = restantes == 0
                    ? $"El documento {numero} fue sincronizado correctamente ({pendientes.Count} ítems)."
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

    public class InicialDto
    {
        public int Id { get; set; }
        public string Numero { get; set; } = "";
        public DateTime FechaCreacion { get; set; }
        public string Comentario { get; set; } = "";
        public int Status { get; set; }
        public ICollection<DetalleInicialDto> Detalles { get; set; } = new List<DetalleInicialDto>();
    }

    public class DetalleInicialDto
    {
        public string ProductCode { get; set; } = "";
        public string ProductName { get; set; } = "";
        public int Cantidad { get; set; }
        public int CantidadFisica { get; set; }
        public string Ubicacion { get; set; } = "";
        public decimal Costo { get; set; }
        public string Categoria { get; set; } = "";
        public string Unidad { get; set; } = "";
        public string Nota { get; set; } = "";
        public bool Procesado { get; set; }
    }

    public class CargaInicialSaveResultDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
    }
}