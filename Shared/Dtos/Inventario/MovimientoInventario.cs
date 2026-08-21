namespace Shared.Dtos.Inventario
{
    public class MovimientoInventario
    {
        public string Numero { get; set; } = "";
        public string TipoDocumento { get; set; } = "";
        public DateTime Fecha { get; set; }
        public DateTime? FechaProcesado { get; set; }
        public string Proveedor { get; set; } = "";
        public string TipoMovimiento { get; set; } = ""; //Entrada / Salida / Ajuste / Transferencia
        public string ProductCode { get; set; } = "";
        public string ProductName { get; set; } = "";
        public int Cantidad { get; set; }
        public decimal Costo { get; set; }
        public decimal Subtotal { get; set; }
        public double StockAnterior { get; set; }
        public double StockNuevo { get; set; }
        public string Usuario { get; set; } = "";
    }

    public class MovimientosProductoResult
    {
        public int ProductId { get; set; }
        public string ProductCode { get; set; } = "";
        public string ProductName { get; set; } = "";
        public double StockActual { get; set; }
        public int TotalCantidad { get; set; }
        public decimal TotalSubtotal { get; set; }
        public List<MovimientoInventario> Movimientos { get; set; } = [];
    }
}
