namespace Shared.Dtos.Compras
{
    /// <summary>
    /// Linea de recepcion enviada por el dispositivo: la cantidad TOTAL recibida
    /// hasta el momento de esa linea (no el delta). El servidor calcula el delta
    /// contra CantidadRecibida ya almacenada, de modo que reenviar lo mismo no
    /// vuelve a afectar el inventario.
    /// </summary>
    public class LineaRecepcionCompra
    {
        public string Product_id { get; set; } = "";
        public double CantidadRecibida { get; set; }
    }

    /// <summary>
    /// Cuerpo del POST api/inventario/process-compra/{numero}: declara lo
    /// recibido hasta el momento, replicando el cierre por escaneos del picking.
    /// </summary>
    public class RecepcionCompraRequest
    {
        public string? Usuario { get; set; }
        public List<LineaRecepcionCompra> Items { get; set; } = [];
    }
}
