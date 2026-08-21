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
        public bool Procesado { get; set; } //true si la linea ya incremento el inventario
        public DateTime? FechaProcesado { get; set; } //cuando se proceso la linea
        [JsonIgnore]
        public OrdenCompra? Order { get; set; }
    }
}
