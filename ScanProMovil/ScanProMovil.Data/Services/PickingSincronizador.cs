using Microsoft.EntityFrameworkCore;
using ScanProMovil.Data.Entities;
using Shared.Dtos.Picking;
using System.Collections.ObjectModel;

// Alias: la entidad local y el DTO compartido se llaman igual.
using EntidadPicking = ScanProMovil.Data.Entities.PickingOrder;
using EntidadDetalle = ScanProMovil.Data.Entities.PickingOrderDetail;

namespace ScanProMovil.Data.Services
{
    /// <summary>
    /// Sincroniza las ordenes de picking del dispositivo con las del servidor.
    /// Vive en el proyecto de datos (no depende de Android) para poder
    /// probarse de forma automatica.
    /// </summary>
    public static class PickingSincronizador
    {
        /// <summary>
        /// Convierte las ordenes del servidor en entidades locales. El Id del
        /// servidor es la clave primaria local, para poder cerrarlas alli.
        /// </summary>
        public static List<EntidadPicking> Mapear(List<PickingPendienteDto> remotos)
        {
            var resultado = new List<EntidadPicking>();

            foreach (var remoto in remotos)
            {
                var pedido = remoto.Pedido;

                var picking = new EntidadPicking
                {
                    PickingId = remoto.PickingId,
                    NumeroPedidoVenta = remoto.NumeroPedidoVenta,
                    Estado = remoto.Estado,
                    FechaCreacion = remoto.FechaCreacion,
                    UsuarioAsignado = remoto.UsuarioAsignado ?? pedido.Vendedor,
                    ClienteNombre = pedido.Cliente_Nombre,
                    FechaCompletado = remoto.FechaCompletado,
                    Items = new ObservableCollection<EntidadDetalle>()
                };

                foreach (var item in pedido.Items)
                {
                    picking.Items.Add(new EntidadDetalle
                    {
                        PickingId = remoto.PickingId,
                        ProductCode = item.Product_id,
                        ProductName = item.Product_name,
                        Cantidad = item.Cantidad,
                        Completado = false
                    });
                }

                resultado.Add(picking);
            }

            return resultado;
        }

        /// <summary>
        /// Aplica las ordenes del servidor sobre la base local: inserta las
        /// nuevas, actualiza las existentes sin perder el avance de los items
        /// ya marcados y cierra las que el servidor ya no lista.
        /// </summary>
        public static async Task<PickingSincronizacionResult> AplicarAsync(
            AppDbContext context,
            List<EntidadPicking> remotos,
            List<int> idsRemotos)
        {
            var resultado = new PickingSincronizacionResult();

            var locales = await context.PickingOrders
                .Include(p => p.Items)
                .Where(p => idsRemotos.Contains(p.PickingId))
                .ToListAsync();

            var localesPorId = locales.ToDictionary(p => p.PickingId);

            foreach (var remoto in remotos)
            {
                if (localesPorId.TryGetValue(remoto.PickingId, out var local))
                {
                    // Ya existe: se actualiza el estado sin perder el avance.
                    if (local.Estado != 2)
                    {
                        local.Estado = remoto.Estado;
                        local.UsuarioAsignado = remoto.UsuarioAsignado ?? local.UsuarioAsignado;
                        local.ClienteNombre = remoto.ClienteNombre ?? local.ClienteNombre;
                        local.FechaCreacion = remoto.FechaCreacion;
                    }
                    else if (remoto.Estado != 2)
                    {
                        // Completada aqui pero el servidor aun no lo sabe.
                        resultado.PendientesDeCerrarRemoto.Add(local);
                    }

                    var codigosLocales = local.Items.Select(i => i.ProductCode).ToHashSet();
                    foreach (var item in remoto.Items)
                    {
                        if (codigosLocales.Contains(item.ProductCode)) continue;

                        local.Items.Add(new EntidadDetalle
                        {
                            PickingId = local.PickingId,
                            ProductCode = item.ProductCode,
                            ProductName = item.ProductName,
                            Cantidad = item.Cantidad,
                            Completado = false
                        });
                    }
                }
                else
                {
                    context.PickingOrders.Add(remoto);
                }
            }

            await context.SaveChangesAsync();

            // Las ordenes que el servidor ya no lista (las completo o cancelo
            // alla) se cierran tambien aqui: si no, se quedarian pendientes
            // para siempre en el dispositivo.
            var idsObsoletos = await context.PickingOrders
                .Where(p => p.Estado != 2 && !idsRemotos.Contains(p.PickingId))
                .Select(p => p.PickingId)
                .ToListAsync();

            if (idsObsoletos.Count > 0)
            {
                var obsoletas = await context.PickingOrders
                    .Where(p => idsObsoletos.Contains(p.PickingId))
                    .ToListAsync();

                foreach (var picking in obsoletas)
                {
                    picking.Estado = 2;
                    picking.FechaCompletado = DateTime.Now;
                }

                await context.SaveChangesAsync();
                resultado.CerradasLocalmente = obsoletas.Count;
            }

            resultado.Insertadas = remotos.Count(r => !localesPorId.ContainsKey(r.PickingId));
            resultado.Actualizadas = remotos.Count - resultado.Insertadas;

            return resultado;
        }
    }

    public class PickingSincronizacionResult
    {
        /// <summary>Ordenes que ya estaban completas aqui y hay que cerrar en el servidor.</summary>
        public List<EntidadPicking> PendientesDeCerrarRemoto { get; } = [];

        public int Insertadas { get; set; }
        public int Actualizadas { get; set; }

        /// <summary>Ordenes locales que el servidor ya no lista y se cerraron aqui.</summary>
        public int CerradasLocalmente { get; set; }
    }
}
