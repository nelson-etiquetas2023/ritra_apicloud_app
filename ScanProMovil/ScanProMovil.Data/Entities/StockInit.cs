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

        [JsonIgnore]
        public int PendientesCount => Items?.Count(i => !i.Enviado) ?? 0;

        [JsonIgnore]
        public string ItemsSummary => Items is { Count: > 0 }
            ? $"{Items.Count} ítems / {Items.Count - PendientesCount} Sincronizado"
            : "0 ítems / 0 Sincronizado";
    }
}