using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shared.Dtos.Picking
{
    public class PickingOrder
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "El número de pedido de venta es obligatorio.")]
        public string NumeroPedidoVenta { get; set; } = "";

        public int Estado { get; set; } // 0 = Pendiente, 1 = Asignado, 2 = Completado

        public string? UsuarioAsignado { get; set; }

        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        public DateTime? FechaCompletado { get; set; }

        [JsonIgnore]
        public string EstadoTexto => Estado switch
        {
            0 => "Pendiente",
            1 => "Asignado",
            2 => "Completado",
            _ => Estado.ToString()
        };
    }

    public class TransferirAPickingRequest
    {
        public List<string> NumerosPedido { get; set; } = [];
        public string? Usuario { get; set; }
    }

    public class PickingOrderResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public List<PickingOrder> Data { get; set; } = [];
    }
}
