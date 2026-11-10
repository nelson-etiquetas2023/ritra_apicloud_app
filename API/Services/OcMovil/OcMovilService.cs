using API.Data;
using Microsoft.EntityFrameworkCore;
using Shared.Dtos.Compras;

namespace API.Services.OcMovil
{
    public class OcMovilService(ApplicationDbContext context) : IOcMovilService
    {
        private readonly ApplicationDbContext context = context;

        public async Task<IEnumerable<OrdenCompra>> GetOrdersAsync()
        {
            var orders = await context.Compra
                .Include(o => o.Items)
                .OrderByDescending(o => o.FechaCreacion)
                .ThenByDescending(o => o.Numero)
                .ToListAsync();

            // Normalizar datos antiguos: una orden con todas sus cantidades
            // recibidas debe mostrarse como Recibido (3), nunca como Ubicado (4).
            var normalizadas = orders
                .Where(o => o.Status == 4 && o.Items.Count > 0 &&
                    o.Items.All(i => i.Cantidad <= 0 ||
                        i.CantidadRecibida >= i.Cantidad ||
                        (i.Procesado && i.CantidadRecibida == 0)))
                .ToList();

            if (normalizadas.Count > 0)
            {
                foreach (var order in normalizadas)
                    order.Status = 3;

                await context.SaveChangesAsync();
            }

            return orders;
        }

        public async Task<OrdenCompra> GetOrderByIdAsync(string numero)
        {
            var oc = await context.Compra.Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Numero == numero);

            return oc is null ? throw new KeyNotFoundException($"Orden {numero} no encontrada") : oc;
        }

        /// <summary>
        /// Genera el siguiente número de orden de compra con formato PO-9999.
        /// PO = Purchase Order, 9999 = consecutivo con 4 dígitos (relleno con ceros a la izquierda).
        /// Ejemplo: PO-0001, PO-0002, ..., PO-9999, PO-10000
        /// </summary>
        public async Task<string> GetNextNumAsync()
        {
            var numeros = await context.Compra
                .AsNoTracking()
                .Select(o => o.Numero)
                .ToListAsync();

            var max = 0;
            foreach (var numero in numeros)
            {
                var match = System.Text.RegularExpressions.Regex.Match(numero ?? "", @"^PO-(\d+)$");
                if (match.Success && int.TryParse(match.Groups[1].Value, out var consecutivo) && consecutivo > max)
                    max = consecutivo;
            }

            return $"PO-{max + 1:0000}";
        }

        public async Task<OrdenCompra?> AddOrderAsync(OrdenCompra oc)
        {
            if (oc is null) return null;

            if (oc.FechaCreacion == default)
                oc.FechaCreacion = DateTime.UtcNow;

            // El servidor debe imponer el formato PO-#### aunque el cliente
            // envíe un número vacío o un formato antiguo como OC-####.
            var numeroTieneFormatoValido = !string.IsNullOrWhiteSpace(oc.Numero)
                && System.Text.RegularExpressions.Regex.IsMatch(
                    oc.Numero.Trim(),
                    @"^PO-\d+$",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            if (!numeroTieneFormatoValido)
            {
                var siguiente = int.Parse((await GetNextNumAsync()).Substring(3));
                while (await context.Compra.AnyAsync(o => o.Numero == $"PO-{siguiente:0000}"))
                    siguiente++;
                oc.Numero = $"PO-{siguiente:0000}";
                oc.Tipo_Documento = "PO"; // Purchase Order
            }
            else
            {
                var consecutivo = int.Parse(oc.Numero.Trim().Substring(3));
                var numeroNormalizado = $"PO-{consecutivo:0000}";
                if (await context.Compra.AnyAsync(o => o.Numero == numeroNormalizado))
                {
                    var siguiente = int.Parse((await GetNextNumAsync()).Substring(3));
                    while (await context.Compra.AnyAsync(o => o.Numero == $"PO-{siguiente:0000}"))
                        siguiente++;
                    numeroNormalizado = $"PO-{siguiente:0000}";
                }

                oc.Numero = numeroNormalizado;
                oc.Tipo_Documento = "PO";
            }

            oc.Prioridad = ValidarPrioridad(oc.Prioridad);
            oc.Status = 1;

            foreach (var item in oc.Items)
            {
                item.Id = 0;
                item.Comentario = item.Comentario?.Trim() ?? string.Empty;
            }

            context.Compra.Add(oc);
            await context.SaveChangesAsync();
            return oc;
        }

        public async Task<OrdenCompra?> UpdateOrderAsync(string Numero, OrdenCompra oc)
        {
            var existingOrder = await context.Compra.Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Numero == Numero);

            if (existingOrder is null) return null;

            //Documento recibido o ubicado: no se permite modificar.
            if (existingOrder.Status == 3 || existingOrder.Status == 4) return null;

            existingOrder.Description = oc.Description;
            existingOrder.Comentario = oc.Comentario;
            existingOrder.Fecha = oc.Fecha;
            if (oc.FechaCreacion != default)
                existingOrder.FechaCreacion = oc.FechaCreacion;
            existingOrder.Supply_Id = oc.Supply_Id;
            existingOrder.Supply_Name = oc.Supply_Name;
            existingOrder.Reference = oc.Reference;
            existingOrder.WarehouseName = oc.WarehouseName;
            existingOrder.Prioridad = ValidarPrioridad(oc.Prioridad);
            // El estado se gestiona segun el flujo:
            //  - Autorizar oleada (web -> móvil) la marca como Por recibir (2).
            //  - Una orden iniciada (1) que se edita permanece iniciada.
            //  - Una orden ya recibida/ubicada no se degrada.
            if (oc.Status == 2)
            {
                existingOrder.Status = 2;
                existingOrder.Sincro = true;
            }
            existingOrder.Subtotal = oc.Subtotal;
            existingOrder.Impuesto = oc.Impuesto;
            existingOrder.Total = oc.Total;
            if (oc.Status != 2)
                existingOrder.Sincro = oc.Sincro;

            await using var transaction = await context.Database.BeginTransactionAsync();
            try
            {
                context.DetalleCompra.RemoveRange(existingOrder.Items);

                var newItems = new List<DetalleCompras>();

                foreach (var item in oc.Items)
                {
                    var newItem = new DetalleCompras
                    {
                        Numero = Numero,

                        Product_id = item.Product_id,
                        Product_name = item.Product_name,
                        Cantidad = item.Cantidad,
                        Costo = item.Costo,
                        Subtotal = item.Subtotal,
                        Comentario = item.Comentario?.Trim() ?? string.Empty,
                    };
                    newItems.Add(newItem);
                }
                await context.DetalleCompra.AddRangeAsync(newItems);
                await context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
            return existingOrder;
        }

        public async Task<bool> AnularOrderAsync(string numero)
        {
            var oc = await context.Compra.FirstOrDefaultAsync(o => o.Numero == numero);
            if (oc == null || oc.Status == 3 || oc.Status == 4) return false;

            // La orden no se anula con un estado fuera de la escala oficial.
            return false;
        }

        public async Task<bool> DeleteOrderAsync(string numero)
        {
            var OrderDeleted = await context.Compra.FindAsync(numero);

            if (OrderDeleted is null) return false;

            //Documento recibido o ubicado: no se permite eliminar.
            if (OrderDeleted.Status == 3 || OrderDeleted.Status == 4) return false;

            context.Compra.Remove(OrderDeleted);
            await context.SaveChangesAsync();
            return true;
        }

        public async Task<int> DeleteAllOrdersAsync()
        {
            await using var transaction = await context.Database.BeginTransactionAsync();
            try
            {
                var eliminadas = await context.Compra.CountAsync();

                // Borrado físico directo: primero los detalles por la FK Numero.
                await context.DetalleCompra.ExecuteDeleteAsync();
                await context.Compra.ExecuteDeleteAsync();
                context.ChangeTracker.Clear();

                await transaction.CommitAsync();
                return eliminadas;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        /// <summary>
        /// Autoriza una orden para recepción en el móvil: cambia su estado a 2 (Por recibir).
        /// Solo se puede autorizar órdenes con estado 1 (Iniciado).
        /// </summary>
        public async Task<bool> AutorizarParaRecepcionAsync(string numero)
        {
            var oc = await context.Compra.FirstOrDefaultAsync(o => o.Numero == numero);
            if (oc is null) return false;

            // Solo se puede autorizar si está Iniciado (1).
            if (oc.Status == 2) return true;
            if (oc.Status != 1) return false;

            oc.Status = 2; // Por recibir / Sincronizado
            oc.Sincro = true;
            await context.SaveChangesAsync();
            return true;
        }

        private static string ValidarPrioridad(string? prioridad)
        {
            return prioridad?.Trim() switch
            {
                "Media" => "Media",
                "Alta" => "Alta",
                "Urgente" => "Urgente",
                _ => "Normal"
            };
        }
    }
}
