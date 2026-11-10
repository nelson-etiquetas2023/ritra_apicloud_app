using Microsoft.EntityFrameworkCore;
using ScanProMovil.Data;
using ScanProMovil.Data.Entities;
using System.Collections.ObjectModel;
using System.Linq;

// Alias: la entidad local y el DTO compartido se llaman igual.
using DtoOrden = Shared.Dtos.Compras.OrdenCompra;
using DtoDetalle = Shared.Dtos.Compras.DetalleCompras;
using EntidadOrden = ScanProMovil.Data.Entities.OrdenCompra;
using EntidadDetalle = ScanProMovil.Data.Entities.DetalleCompra;

namespace ScanProMovil.Data.Services
{
    /// <summary>
    /// Sincroniza las ordenes de compra del servidor sobre la base local sin
    /// perder la recepcion parcial ya escaneada en el dispositivo. Vive en el
    /// proyecto de datos (no depende de Android) para poder probarse de forma
    /// automatica (misma idea de PickingSincronizador).
    /// </summary>
    /// <remarks>
    /// Reglas de estados (server y local usan la misma escala):
        /// 1 Iniciado, 2 Por recibir, 3 Recibido, 4 Ubicado.
    /// </remarks>
    public static class ComprasSincronizador
    {
        /// <summary>
        /// Convierte las ordenes del servidor en entidades locales. El Numero
        /// del servidor es la clave primaria local, para poder recepcionarlas alla.
        /// </summary>
        public static List<EntidadOrden> Mapear(List<DtoOrden> remotos)
        {
            var resultado = new List<EntidadOrden>();

            foreach (var remoto in remotos)
            {
                var orden = new EntidadOrden
                {
                    Numero = remoto.Numero,
                    Fecha = remoto.Fecha,
                    FechaCreacion = remoto.FechaCreacion == default ? DateTime.Now : remoto.FechaCreacion,
                    Description = remoto.Description,
                    Comentario = remoto.Comentario,
                    Status = remoto.Status,
                    Sincro = remoto.Sincro,
                    Tipo_Documento = remoto.Tipo_Documento,
                    Subtotal = remoto.Subtotal,
                    Impuesto = remoto.Impuesto,
                    Total = remoto.Total,
                    Supply_Id = remoto.Supply_Id,
                    Supply_Name = remoto.Supply_Name,
                    Reference = remoto.Reference,
                    UserName = remoto.UserName,
                    UserEmail = remoto.UserEmail,
                    UserRole = remoto.UserRole,
                    DeviceCode = remoto.DeviceCode,
                    DeviceName = remoto.DeviceName,
                    WarehouseName = remoto.WarehouseName,
                    ItemsNumber = remoto.Items?.Count ?? 0,
                    Items = []
                };

                foreach (var item in remoto.Items ?? [])
                {
                    orden.Items.Add(new EntidadDetalle
                    {
                        Numero = item.Numero,
                        Product_id = item.Product_id,
                        Product_Name = item.Product_name,
                        Cantidad = item.Cantidad,
                        Costo = (double)item.Costo,
                        Subtotal = (double)item.Subtotal,
                        // Nueva linea: adopta lo ya recibido en el servidor (otro
                        // dispositivo pudo haber recibido aqui).
                        CantidadRecibida = item.CantidadRecibida
                    });
                }

                resultado.Add(orden);
            }

            return resultado;
        }

        /// <summary>
        /// Aplica las ordenes del servidor sobre la base local: inserta las
        /// nuevas, actualiza las existentes sin perder lo ya recibido local y
        /// reporta las ordenes con recepcion pendiente de propagar al servidor.
        /// No elimina ordenes creadas en el dispositivo (Sincro=false) por mas
        /// que el servidor no las liste.
        /// </summary>
        public static async Task<ComprasSincronizacionResult> AplicarAsync(
            AppDbContext context,
            List<EntidadOrden> remotos,
            List<string> numerosRemotos)
        {
            var resultado = new ComprasSincronizacionResult();

            var locales = await context.PurchaseOrders
                .Include(o => o.Items)
                .Where(o => numerosRemotos.Contains(o.Numero))
                .ToListAsync();

            Console.WriteLine($"[ComprasSincronizador] Recibidos {remotos.Count} remotos; locales existentes con esos numeros: {locales.Count}");

            var localesPorNumero = locales.ToDictionary(o => o.Numero);

            int insertadas = 0;
            int actualizadas = 0;

            foreach (var remoto in remotos)
            {
                if (localesPorNumero.TryGetValue(remoto.Numero, out var local))
                {
                    // Ya existe: se actualiza el encabezado SOLO si la orden local
                    // no esta recibida (3) ni ubicada (4):
                    // cerradas localmente nunca se destapan desde el sync.
                    if (local.Status is 3 or 4)
                    {
                        // Nada que actualizar del encabezado; los items se
                        // resincronizan igual abajo (cantidades editadas alla).
                    }
                    else
                    {
                        local.Fecha = remoto.Fecha;
                        local.Description = remoto.Description;
                        local.Comentario = remoto.Comentario;
                        local.Status = remoto.Status;
                        local.Sincro = remoto.Sincro;
                        local.Tipo_Documento = remoto.Tipo_Documento;
                        local.Subtotal = remoto.Subtotal;
                        local.Impuesto = remoto.Impuesto;
                        local.Total = remoto.Total;
                        local.Supply_Id = remoto.Supply_Id;
                        local.Supply_Name = remoto.Supply_Name;
                        local.Reference = remoto.Reference;
                    }

                    // Sincronizar items: actualizar los existentes (cantidad, nombre
                    // y costos, sin tocar el avance CantidadRecibida local), agregar
                    // los nuevos y quitar los que el servidor ya no envia.
                    var remotoPorProductId = remoto.Items?
                        .Where(i => !string.IsNullOrWhiteSpace(i.Product_id))
                        .ToDictionary(i => i.Product_id, StringComparer.OrdinalIgnoreCase) ?? new Dictionary<string, EntidadDetalle>(StringComparer.OrdinalIgnoreCase);

                    foreach (var localItem in local.Items.ToList())
                    {
                        if (remotoPorProductId.TryGetValue(localItem.Product_id, out var remotoItem))
                        {
                            localItem.Product_Name = remotoItem.Product_Name;
                            localItem.Cantidad = remotoItem.Cantidad;
                            localItem.Costo = remotoItem.Costo;
                            localItem.Subtotal = remotoItem.Subtotal;
                        }
                        else
                        {
                            local.Items.Remove(localItem);
                        }
                    }

                    var idsLocales = local.Items.Select(i => i.Product_id).ToHashSet(StringComparer.OrdinalIgnoreCase);
                    foreach (var remotoItem in remoto.Items ?? [])
                    {
                        if (string.IsNullOrWhiteSpace(remotoItem.Product_id) ||
                            idsLocales.Contains(remotoItem.Product_id)) continue;

                        local.Items.Add(new EntidadDetalle
                        {
                            Numero = local.Numero,
                            Product_id = remotoItem.Product_id,
                            Product_Name = remotoItem.Product_Name,
                            Cantidad = remotoItem.Cantidad,
                            Costo = remotoItem.Costo,
                            Subtotal = remotoItem.Subtotal,
                            CantidadRecibida = remotoItem.CantidadRecibida
                        });
                    }

                    local.ItemsNumber = local.Items.Count;

                    // Pedido al estilo picking: lo recibido aqui (estado 3)
                    // que el servidor todavia no conoce debe propagarse.
                    var totalLocal = local.Items.Sum(i => i.CantidadRecibida);
                    var totalRemoto = (remoto.Items ?? []).Sum(i => (double)i.CantidadRecibida);
                    if (totalLocal > totalRemoto ||
                    (local.Status == 3 && remoto.Status != 3))
                    {
                        resultado.PendientesDeRecepcionRemoto.Add(local);
                    }

                    actualizadas++;
                }
                else
                {
                    context.PurchaseOrders.Add(remoto);
                    insertadas++;
                }
            }

            await context.SaveChangesAsync();

            Console.WriteLine($"[ComprasSincronizador] Aplicadas: insertadas={insertadas}, actualizadas={actualizadas}, pendientesDeRecepcion={resultado.PendientesDeRecepcionRemoto.Count}");

            // Las ordenes sincronizadas que el servidor ya no lista se eliminan
            // localmente. Las creadas en el dispositivo (Sincro=false) jamas se
            // tocan aqui: el servidor puede aun no conocerlas.
            var numerosSet = numerosRemotos.ToHashSet(StringComparer.OrdinalIgnoreCase);
            var obsoletas = await context.PurchaseOrders
                .Include(o => o.Items)
                .Where(o => o.Sincro && !numerosSet.Contains(o.Numero))
                .ToListAsync();

            if (obsoletas.Count > 0)
            {
                Console.WriteLine($"[ComprasSincronizador] Ordenes sincronizadas obsoletas (servidor ya no las lista): {obsoletas.Count}");
                context.PurchaseOrders.RemoveRange(obsoletas);
                await context.SaveChangesAsync();
                resultado.CerradasLocalmente = obsoletas.Count;
            }
            else
            {
                Console.WriteLine("[ComprasSincronizador] No hay ordenes obsoletas.");
            }

            resultado.Insertadas = insertadas;
            resultado.Actualizadas = actualizadas;

            return resultado;
        }
    }

    public class ComprasSincronizacionResult
    {
        /// <summary>Ordenes con unidades ya recibidas aqui que el servidor todavia no conoce.</summary>
        public List<EntidadOrden> PendientesDeRecepcionRemoto { get; } = [];

        public int Insertadas { get; set; }
        public int Actualizadas { get; set; }

        /// <summary>Ordenes sincronizadas que el servidor ya no lista y se eliminaron aqui.</summary>
        public int CerradasLocalmente { get; set; }
    }
}
