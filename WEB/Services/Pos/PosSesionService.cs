using Shared.Dtos.Ventas;

namespace WEB.Services.Pos
{
    public class VentaSesion
    {
        public int PedidoId { get; set; }
        public string Numero { get; set; } = "";
        public DateTime Fecha { get; set; } = DateTime.Now;
        public string ClienteNombre { get; set; } = "";
        public string Estado { get; set; } = "";
        public List<DetalleVenta> Items { get; set; } = [];
        public List<VentaPago> Pagos { get; set; } = [];
        public decimal Subtotal { get; set; }
        public decimal Descuento { get; set; }
        public decimal Impuesto { get; set; }
        public decimal Total { get; set; }
        public decimal Recibido { get; set; }
        public decimal Cambio { get; set; }
        public int Unidades => Items.Sum(i => i.Cantidad);

        public string ResumenPagos => Pagos.Count == 0
            ? ""
            : string.Join(" · ", Pagos.Select(g => $"{AbrevMetodo(g.Metodo)} {g.Monto:N2}"));

        private static string AbrevMetodo(string m) => m switch
        {
            MetodoPago.Efectivo => "Ef",
            MetodoPago.Tarjeta => "Tj",
            MetodoPago.Transferencia => "Tr",
            _ => m
        };
    }

    // Vive en memoria durante la sesión del navegador (scoped en WASM).
    // Cada cobro exitoso del POS agrega aquí su ticket para la lista de ventas.
    public class PosSesionService
    {
        public List<VentaSesion> Ventas { get; } = [];

        public void Agregar(VentaSesion venta) => Ventas.Insert(0, venta);

        public void Limpiar() => Ventas.Clear();

        public decimal TotalVendido => Ventas.Sum(v => v.Total);

        public int TotalUnidades => Ventas.Sum(v => v.Unidades);

        // Vista del turno: ventas guardadas en BD (historial del turno) con las de la
        // sesión encima por número (conservan Recibido/Cambio para reimprimir el ticket igual).
        public List<VentaSesion> VistaTurno(IEnumerable<PedidoVenta> pedidos)
        {
            var sesion = Ventas.ToDictionary(v => v.Numero);
            return pedidos.Select(p => sesion.TryGetValue(p.Numero, out var s) ? s : new VentaSesion
            {
                PedidoId = p.Id,
                Numero = p.Numero,
                Fecha = p.Fecha,
                ClienteNombre = p.Cliente_Nombre,
                Estado = p.StatusTexto,
                Items = p.Items.ToList(),
                Subtotal = p.Subtotal,
                Descuento = p.Descuento,
                Impuesto = p.Impuesto,
                Total = p.Total,
                Recibido = p.Total,
                Cambio = 0,
                Pagos = p.Pagos.Count > 0
                    ? p.Pagos.ToList()
                    : [new VentaPago { Metodo = MetodoPago.Efectivo, Monto = p.Total }]
            }).ToList();
        }
    }
}
