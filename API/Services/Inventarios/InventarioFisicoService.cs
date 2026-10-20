using API.Data;
using Microsoft.EntityFrameworkCore;
using Shared.Dtos.Inventarios;

namespace API.Services.Inventarios
{
    public class InventarioFisicoService(ApplicationDbContext context) : IInventarioFisicoService
    {
        private const int StatusActualizado = 1;
        private const int StatusAplicado = 4;

        private readonly ApplicationDbContext context = context;

        public async Task<List<InventarioFisico>> GetAllAsync()
        {
            return await context.InventarioFisico
                .Include(d => d.Detalles)
                .OrderByDescending(d => d.FechaCreacion)
                .ThenByDescending(d => d.Numero)
                .ToListAsync();
        }

        public async Task<InventarioFisico?> GetByIdAsync(int id)
        {
            return await context.InventarioFisico
                .Include(d => d.Detalles)
                .FirstOrDefaultAsync(d => d.Id == id);
        }

        public async Task<FiscalSaveResult> CreateAsync(InventarioFisico documento)
        {
            var result = new FiscalSaveResult();

            if (documento.Detalles == null || documento.Detalles.Count == 0)
            {
                result.Success = false;
                result.Message = "El documento no tiene líneas de detalle.";
                return result;
            }

            if (documento.FechaCreacion == default)
                documento.FechaCreacion = DateTime.UtcNow;

            documento.Status = StatusActualizado;

            foreach (var detalle in documento.Detalles)
            {
                detalle.Id = 0;
                detalle.Procesado = false;
                detalle.FechaProcesado = null;
            }

            // El correlativo lo asigna el servidor e ignora el que envie el movil.
            // Este genera un numero local temporal (TMP-####) porque el conteo
            // ocurre sin conexion, y lo re-clavea con el valor devuelto aqui.
            // Asignar e insertar comparten transaccion para que dos equipos que
            // sincronicen a la vez no obtengan el mismo correlativo.
            await using var transaction = await context.Database
                .BeginTransactionAsync(System.Data.IsolationLevel.Serializable);

            try
            {
                documento.Numero = await CalcularSiguienteNumeroAsync();

                context.InventarioFisico.Add(documento);
                await context.SaveChangesAsync();

                await transaction.CommitAsync();
            }
            catch (DbUpdateException)
            {
                await transaction.RollbackAsync();

                result.Success = false;
                result.Message = "Ya existe un documento físico con ese correlativo. Intente nuevamente.";
                return result;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }

            result.Success = true;
            result.Message = $"Documento físico {documento.Numero} guardado correctamente.";
            result.Data = documento;
            return result;
        }

        public async Task<FiscalSaveResult> UpdateAsync(int id, InventarioFisico documento)
        {
            var result = new FiscalSaveResult();

            var existing = await context.InventarioFisico
                .Include(d => d.Detalles)
                .FirstOrDefaultAsync(d => d.Id == id);
            if (existing == null)
            {
                result.Success = false;
                result.Message = $"El documento físico {id} no fue encontrado.";
                return result;
            }

            if (existing.Status == StatusAplicado)
            {
                result.Success = false;
                result.Message = "El documento ya fue aplicado al inventario y no puede editarse.";
                return result;
            }

            if (documento.Detalles == null || documento.Detalles.Count == 0)
            {
                result.Success = false;
                result.Message = "El documento no tiene líneas de detalle.";
                return result;
            }

            existing.Numero = documento.Numero;
            existing.Comentario = documento.Comentario;
            existing.Status = StatusActualizado;

            var incoming = documento.Detalles.ToList();
            foreach (var detalle in incoming)
            {
                var match = existing.Detalles.FirstOrDefault(d =>
                    string.Equals(d.ProductCode?.Trim(), detalle.ProductCode?.Trim(), StringComparison.OrdinalIgnoreCase));

                if (match is not null)
                {
                    match.ProductName = detalle.ProductName;
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
                    detalle.InventarioFisicoId = id;
                    detalle.Procesado = false;
                    detalle.FechaProcesado = null;
                    context.InventarioFisicoDetalles.Add(detalle);
                }
            }

            await context.SaveChangesAsync();

            result.Success = true;
            result.Message = $"Documento físico {existing.Numero} actualizado correctamente.";
            result.Data = await GetByIdAsync(id);
            return result;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var documento = await context.InventarioFisico.FirstOrDefaultAsync(d => d.Id == id);
            if (documento == null) return false;

            context.InventarioFisico.Remove(documento);
            await context.SaveChangesAsync();
            return true;
        }

        public Task<string> GetNextNumAsync()
        {
            return Task.FromResult(BuildNextNum());
        }

        public async Task<ComparacionResult> CompararAsync(string documentoTeorico, string documentoFisico)
        {
            if (string.IsNullOrWhiteSpace(documentoTeorico))
                throw new ArgumentException("Debe indicar el documento teórico a comparar.");
            if (string.IsNullOrWhiteSpace(documentoFisico))
                throw new ArgumentException("Debe indicar el documento físico a comparar.");

            var teorico = await context.InventarioTeorico
                .AsNoTracking()
                .Include(i => i.Detalles)
                .FirstOrDefaultAsync(i => i.Numero.Trim().ToUpper() == documentoTeorico.Trim().ToUpper());

            if (teorico == null)
                throw new ArgumentException($"El documento teórico '{documentoTeorico}' no existe.");

            var fisico = await context.InventarioFisico
                .AsNoTracking()
                .Include(d => d.Detalles)
                .FirstOrDefaultAsync(d => d.Numero.Trim().ToUpper() == documentoFisico.Trim().ToUpper());

            if (fisico == null)
                throw new ArgumentException($"El documento físico '{documentoFisico}' no existe.");

            var teoricoMap = teorico.Detalles
                .Where(d => !string.IsNullOrWhiteSpace(d.ProductCode))
                .GroupBy(d => d.ProductCode.Trim().ToUpper())
                .ToDictionary(g => g.Key, g => g.Sum(d => d.Cantidad));

            var fisicoMap = fisico.Detalles
                .Where(d => !string.IsNullOrWhiteSpace(d.ProductCode))
                .GroupBy(d => d.ProductCode.Trim().ToUpper())
                .ToDictionary(g => g.Key, g => new
                {
                    Cantidad = g.Sum(d => d.CantidadFisica),
                    Ubicacion = g.Select(d => d.Ubicacion).FirstOrDefault(u => !string.IsNullOrWhiteSpace(u)) ?? ""
                });

            var codigos = teoricoMap.Keys
                .Union(fisicoMap.Keys)
                .OrderBy(c => c, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var nombres = await ResolverNombresProductosAsync(codigos);

            var detalles = new List<ComparacionDetalle>(codigos.Count);
            foreach (var codigo in codigos)
            {
                teoricoMap.TryGetValue(codigo, out var cantidadTeorica);
                fisicoMap.TryGetValue(codigo, out var fisicoInfo);

                var enTeorico = teoricoMap.ContainsKey(codigo);
                var enFisico = fisicoInfo != null;

                nombres.TryGetValue(codigo, out var nombreProducto);

                detalles.Add(new ComparacionDetalle
                {
                    ProductCode = codigo,
                    ProductName = nombreProducto ?? codigo,
                    CantidadTeorica = cantidadTeorica,
                    CantidadFisica = fisicoInfo?.Cantidad ?? 0,
                    SoloTeorico = enTeorico && !enFisico,
                    SoloFisico = enFisico && !enTeorico,
                    Ubicacion = fisicoInfo?.Ubicacion ?? ""
                });
            }

            return new ComparacionResult
            {
                DocumentoTeorico = teorico.Numero,
                InventarioFisico = fisico.Numero,
                Detalles = detalles
            };
        }

        public async Task<AplicarFisicoResult> AplicarAsync(int id, string? documentoTeorico)
        {
            var result = new AplicarFisicoResult();

            var documento = await context.InventarioFisico
                .Include(d => d.Detalles)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (documento == null)
            {
                result.Success = false;
                result.Message = $"El documento físico {id} no existe.";
                return result;
            }

            result.InventarioFisico = documento.Numero;

            if (documento.Status == StatusAplicado)
            {
                result.Success = false;
                result.Message = $"El documento {documento.Numero} ya fue aplicado al inventario el {documento.FechaAplicacion:dd/MM/yyyy HH:mm}.";
                return result;
            }

            if (documento.Detalles.Count == 0)
            {
                result.Success = false;
                result.Message = $"El documento {documento.Numero} no tiene líneas de detalle.";
                return result;
            }

            var conciliado = !string.IsNullOrWhiteSpace(documentoTeorico);

            InventarioTeorico? teorico = null;
            if (conciliado)
            {
                teorico = await context.InventarioTeorico
                    .Include(i => i.Detalles)
                    .FirstOrDefaultAsync(i => i.Numero.Trim().ToUpper() == documentoTeorico!.Trim().ToUpper());

                if (teorico == null)
                {
                    result.Success = false;
                    result.Message = $"El documento teórico '{documentoTeorico}' no existe.";
                    return result;
                }

                result.DocumentoTeorico = teorico.Numero;
            }

            var ahora = DateTime.UtcNow;

            // Un mismo producto puede venir repetido en varias líneas: se suman antes de fijar el stock.
            var cantidadesFisicas = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            foreach (var detalle in documento.Detalles)
            {
                if (string.IsNullOrWhiteSpace(detalle.ProductCode)) continue;

                var codigo = detalle.ProductCode.Trim().ToUpper();
                cantidadesFisicas[codigo] = cantidadesFisicas.TryGetValue(codigo, out var previa)
                    ? previa + detalle.CantidadFisica
                    : detalle.CantidadFisica;
            }

            var codigosEscaneados = cantidadesFisicas.Keys
                .OrderBy(c => c, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var codigosTeoricos = new List<string>();
            if (conciliado && teorico is not null)
            {
                codigosTeoricos = teorico.Detalles
                    .Where(d => !string.IsNullOrWhiteSpace(d.ProductCode))
                    .Select(d => d.ProductCode.Trim().ToUpper())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Where(c => !cantidadesFisicas.ContainsKey(c))
                    .OrderBy(c => c, StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }

            await using var transaction = await context.Database.BeginTransactionAsync();

            // Una sola consulta para todos los productos en lugar de una por línea.
            var productos = await ResolverProductosAsync(codigosEscaneados.Concat(codigosTeoricos).ToList());

            foreach (var codigo in codigosEscaneados)
            {
                if (!productos.TryGetValue(codigo, out var producto))
                {
                    result.Errors.Add(new RowError
                    {
                        Row = 0,
                        Message = $"El producto '{codigo}' no existe en el catálogo. No se aplicó ningún cambio."
                    });
                    continue;
                }

                var stockAnterior = producto.Stock;
                producto.Stock = cantidadesFisicas[codigo];

                result.Lineas.Add(new AplicacionLinea
                {
                    ProductCode = producto.Codebar ?? producto.Product_Code,
                    ProductName = producto.Product_Name,
                    CantidadFisica = cantidadesFisicas[codigo],
                    StockAnterior = stockAnterior,
                    StockNuevo = producto.Stock
                });
            }

            if (conciliado && teorico is not null)
            {
                foreach (var codigo in codigosTeoricos)
                {
                    if (!productos.TryGetValue(codigo, out var producto))
                    {
                        result.Errors.Add(new RowError
                        {
                            Row = 0,
                            Message = $"El producto '{codigo}' está en el teórico {teorico.Numero} pero no existe en el catálogo."
                        });
                        continue;
                    }

                    var stockAnterior = producto.Stock;
                    producto.Stock = 0;

                    result.Lineas.Add(new AplicacionLinea
                    {
                        ProductCode = producto.Codebar ?? producto.Product_Code,
                        ProductName = producto.Product_Name,
                        CantidadFisica = 0,
                        StockAnterior = stockAnterior,
                        StockNuevo = 0,
                        PuestoEnCero = true
                    });
                }
            }

            if (result.Errors.Count > 0)
            {
                await transaction.RollbackAsync();
                result.Success = false;
                result.Lineas.Clear();
                result.Message = "No se aplicó el documento porque hay productos que no existen en el catálogo.";
                return result;
            }

            foreach (var detalle in documento.Detalles)
            {
                detalle.Procesado = true;
                detalle.FechaProcesado = ahora;
            }

            documento.Status = StatusAplicado;
            documento.FechaAplicacion = ahora;
            if (conciliado && teorico is not null)
                documento.DocumentoTeorico = teorico.Numero;

            await context.SaveChangesAsync();
            await transaction.CommitAsync();

            result.Success = true;
            result.FechaAplicacion = ahora;
            result.ProductosActualizados = result.Lineas.Count(l => !l.PuestoEnCero);
            result.ProductosPuestosEnCero = result.Lineas.Count(l => l.PuestoEnCero);
            result.Message = conciliado
                ? $"El conteo {documento.Numero} se aplicó al inventario conciliado con {teorico!.Numero}: {result.ProductosActualizados} producto(s) actualizados y {result.ProductosPuestosEnCero} puesto(s) en cero."
                : $"El conteo {documento.Numero} se aplicó al inventario: {result.ProductosActualizados} producto(s) actualizados.";

            return result;
        }

        private async Task<Dictionary<string, Shared.Dtos.Product>> ResolverProductosAsync(List<string> codigos)
        {
            var productos = new Dictionary<string, Shared.Dtos.Product>(StringComparer.OrdinalIgnoreCase);
            if (codigos.Count == 0) return productos;

            var encontrados = await context.Productos
                .Where(p => (p.Codebar != null && codigos.Contains(p.Codebar.ToUpper())) ||
                            (p.Product_Code != null && codigos.Contains(p.Product_Code.ToUpper())))
                .ToListAsync();

            foreach (var producto in encontrados)
            {
                if (!string.IsNullOrWhiteSpace(producto.Codebar))
                    productos[producto.Codebar.Trim().ToUpper()] = producto;
                if (!string.IsNullOrWhiteSpace(producto.Product_Code))
                    productos[producto.Product_Code.Trim().ToUpper()] = producto;
            }

            return productos;
        }

        private async Task<Dictionary<string, string>> ResolverNombresProductosAsync(List<string> codigos)
        {
            var nombres = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (codigos.Count == 0) return nombres;

            var productos = await context.Productos
                .Where(p => (p.Codebar != null && codigos.Contains(p.Codebar.ToUpper())) ||
                            (p.Product_Code != null && codigos.Contains(p.Product_Code.ToUpper())))
                .Select(p => new { p.Codebar, p.Product_Code, p.Product_Name })
                .ToListAsync();

            foreach (var producto in productos)
            {
                if (!string.IsNullOrWhiteSpace(producto.Codebar))
                    nombres[producto.Codebar.Trim()] = producto.Product_Name;
                if (!string.IsNullOrWhiteSpace(producto.Product_Code))
                    nombres[producto.Product_Code.Trim()] = producto.Product_Name;
            }

            return nombres;
        }

        private string BuildNextNum()
        {
            const string prefix = "FIS-";

            var numeros = context.InventarioFisico
                .AsNoTracking()
                .Select(d => d.Numero)
                .ToList();

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

        private async Task<string> CalcularSiguienteNumeroAsync()
        {
            // UPDLOCK + HOLDLOCK sobre el agregado retiene el rango hasta el
            // commit, de modo que dos equipos sincronizando a la vez no puedan
            // leer el mismo max. El indice unico queda como segunda barrera.
            var siguiente = await context.Database
                .SqlQuery<int>(
                    // String interpolada porque SqlQuery<TResult> espera
                    // FormattableString. Sin llaves en el SQL.
                    $"""
                    SELECT ISNULL(MAX(TRY_CAST(SUBSTRING([Numero], 5, 16) AS int)), 0) + 1 AS [Value]
                    FROM [InventarioFisico] WITH (UPDLOCK, HOLDLOCK)
                    WHERE [Numero] LIKE 'FIS-[0-9]%'
                    """)
                .SingleAsync();

            return $"FIS-{siguiente:D4}";
        }
    }
}
