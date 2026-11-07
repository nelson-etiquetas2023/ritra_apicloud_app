using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shared.Dtos.Compras
{
    public class DetalleCompras
    {
        [Key]
        public int Id { get; set; } //PK
        public string Numero { get; set; } = "";
        public string Product_id { get; set; } = "";
        public string Product_name { get; set; } = "";
        public int Cantidad { get; set; }
        public decimal Costo { get; set; }
        public decimal Subtotal { get; set; }
        public string Comentario { get; set; } = "";
        public bool Procesado { get; set; } //true cuando la linea se recibio completa (Cargo el inventario)
        public DateTime? FechaProcesado { get; set; } //cuando se proceso la linea
        // Recepcion parcial: unidades ya recibidas y cargadas a inventario en el servidor.
        // El servidor es la fuente de verdad; el movil lo actualiza al enviar la recepcion.
        public int CantidadRecibida { get; set; }
        public DateTime? FechaRecepcion { get; set; } //ultima actualizacion de la recepcion
        [JsonIgnore]
        public OrdenCompra? Order { get; set; }
    }
}
