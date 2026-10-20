using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ScanProMovil.Data.Entities
{
    public class PickingOrder
    {
        [Key]
        public int PickingId { get; set; }

        public string NumeroPedidoVenta { get; set; } = null!;

        public int Estado { get; set; } // 0 = Pendiente, 1 = Asignado, 2 = Completado

        public DateTime FechaCreacion { get; set; }

        public string? UsuarioAsignado { get; set; }

        public DateTime? FechaCompletado { get; set; }

        public string EstadoTexto =>
            Estado switch
            {
                0 => "Pendiente",
                1 => "Asignado",
                2 => "Completado",
                _ => Estado.ToString()
            };

        public ObservableCollection<PickingOrderDetail> Items { get; set; } = [];
    }

    public class PickingOrderDetail
    {
        public int DetailId { get; set; }

        public int PickingId { get; set; }

        [JsonIgnore]
        public PickingOrder? PickingOrder { get; set; }

        public string ProductCode { get; set; } = null!;

        public string ProductName { get; set; } = null!;

        public int Cantidad { get; set; }

        public bool Completado { get; set; }
    }
}
