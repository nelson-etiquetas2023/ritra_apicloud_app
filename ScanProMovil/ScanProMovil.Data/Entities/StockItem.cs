using System.Text.Json.Serialization;

namespace ScanProMovil.Entities
{
    public class StockItem
    {
        [JsonIgnore]
        public int Id { get; set; }
        public string Numero { get; set; } = null!;
        public string Product_Code { get; set; } = null!;
        public string Product_Name { get; set; } = null!;
        public double Cantidad { get; set; } = 0;
        public double Costo { get; set; } = 0;
        public double TotalCosto { get; set; } = 0;
        public string Ubicacion { get; set; } = null!;
        public string Nota { get; set; } = string.Empty;
        public bool Enviado { get; set; }
    }
}