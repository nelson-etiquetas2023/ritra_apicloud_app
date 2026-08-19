using System.Text.Json.Serialization;

namespace ScanProMovil.Data.Entities
{
    public class DetalleCompra
    {
        [JsonIgnore]
        public int DetailId { get; set; }
        public int OrderId { get; set; }
        public string Numero { get; set; } = null!;
        public string Product_id { get; set; } = null!;
        public string Product_Name { get; set; } = null!;
        public double Cantidad { get; set; }
        public double Costo { get; set; } = 0;
        public double Subtotal { get; set; }

    }
}
