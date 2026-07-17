using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shared.Dtos.Compras
{
    public class DetalleCompras
    {
        [Key]
        public int Id { get; set; } //PK
        public string Numero { get; set; } = "";
        public int Produc_id { get; set; }
        public string Product_name { get; set; } = "";
        public int Cantidad { get; set; }
        public decimal Costo { get; set; }
        public decimal Subtotal { get; set; }
        [JsonIgnore]
        public OrdenCompra? Order { get; set; }
    }
}
