using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ScanProMovil.Entities
{
    public class StockInit
    {
        public string Numero { get; set; } = null!;
        public DateTime Fecha { get; set; }
        public string Document_Teorico { get; set; } = null!;
        public string Description { get; set; } = string.Empty;
        public string Status { get; set; } = null!;
        [JsonIgnore]
        public List<StockItem> Items { get; set; } = new();
    }
}