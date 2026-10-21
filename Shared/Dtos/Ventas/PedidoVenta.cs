using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Shared.Dtos.Inventarios;

namespace Shared.Dtos.Ventas
{
    /// <summary>
    /// Estados posibles de un pedido de venta.
    /// </summary>
    public static class PedidoVentaStatus
    {
        public const int Planificado = 0;
        public const int Modificado = 1;
        public const int Sincronizado = 2;
        public const int Cerrado = 3;
        public const int Procesado = 4;
        public const int TransaccionFallida = 5;
        public const int Anulado = 6;
        public const int Picking = 7;

        public static string Texto(int status) => status switch
        {
            Planificado => "Planificado",
            Modificado => "Modificado",
            Sincronizado => "Sincronizado",
            Cerrado => "Cerrado",
            Procesado => "Procesado",
            TransaccionFallida => "Transacción Fallida",
            Anulado => "Anulado",
            Picking => "Picking",
            _ => status.ToString()
        };
    }

    public class PedidoVenta
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "El pedido debe tener un número que lo identifique.")]
        public string Numero { get; set; } = "";

        public DateTime Fecha { get; set; } = DateTime.Now;

        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        public int Cliente_Id { get; set; }

        public string Cliente_Nombre { get; set; } = "";

        public string Cliente_RNC { get; set; } = "";

        public string DireccionEntrega { get; set; } = "";

        public string Vendedor { get; set; } = "";

        public string Prioridad { get; set; } = "Normal";

        public string WarehouseName { get; set; } = "";

        public string Reference { get; set; } = "";

        public string Description { get; set; } = "";

        public int Status { get; set; }

        public int? TurnoId { get; set; }

        public decimal Subtotal { get; set; }

        public decimal Descuento { get; set; }

        public decimal Impuesto { get; set; }

        public decimal Total { get; set; }

        public ICollection<DetalleVenta> Items { get; set; } = [];

        public ICollection<VentaPago> Pagos { get; set; } = [];

        [JsonIgnore]
        public string StatusTexto => PedidoVentaStatus.Texto(Status);
    }

    public class DetalleVenta
    {
        [Key]
        public int Id { get; set; }

        public int PedidoVentaId { get; set; }

        [JsonIgnore]
        public PedidoVenta? PedidoVenta { get; set; }

        public string Product_id { get; set; } = "";

        public string Product_name { get; set; } = "";

        public int Cantidad { get; set; }

        public decimal Precio { get; set; }

        public decimal Descuento { get; set; }

        public decimal Stock { get; set; }

        public string Comentario { get; set; } = "";

        public bool Procesado { get; set; }

        public DateTime? FechaProcesado { get; set; }

        [JsonIgnore]
        public decimal Subtotal => Cantidad * Precio;

        [JsonIgnore]
        public decimal Importe => Subtotal - Subtotal * (Descuento / 100m);
    }

    public static class MetodoPago
    {
        public const string Efectivo = "Efectivo";
        public const string Tarjeta = "Tarjeta";
        public const string Transferencia = "Transferencia";
    }

    public class VentaPago
    {
        [Key]
        public int Id { get; set; }

        public int PedidoVentaId { get; set; }

        [JsonIgnore]
        public PedidoVenta? PedidoVenta { get; set; }

        public string Metodo { get; set; } = MetodoPago.Efectivo;

        public decimal Monto { get; set; }

        public string Referencia { get; set; } = "";
    }

    public class PedidoVentaSaveResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public PedidoVenta? Data { get; set; }
        public List<RowError> Errors { get; set; } = [];
    }

    public class ProcesarPedidoResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public long ElapsedMilliseconds { get; set; }
        public int StatusFinal { get; set; }
        public List<ProcesarItemVentaResult> Items { get; set; } = [];
    }

    public class ProcesarItemVentaResult
    {
        public string ProductCode { get; set; } = "";
        public string ProductName { get; set; } = "";
        public int Cantidad { get; set; }
        public double StockAnterior { get; set; }
        public double StockNuevo { get; set; }
        public bool Ok { get; set; }
        public string Error { get; set; } = "";
        public bool YaProcesado { get; set; }
    }
}
