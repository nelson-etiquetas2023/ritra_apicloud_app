using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shared.Dtos
{
    public class OrderFisicoHeader
    {
        [Key]
        public string OrderNumberID { get; set; } = null!;
        public DateTime Order_Date { get; set; }
        public string Description_Document { get; set; } = null!;
        public string Order_Hour { get; set; } = null!;
        public string Notes { get; set; } = null!;
        public string Person_Create { get; set; } = null!;
        public string Status { get; set; } =  null!;
        public string Status_Name { get; set; } = null!;
        public int Items { get; set; }
        public int EquiposConteo { get; set; } = 0;
        public bool Document_Anulado { get; set; }
        public string Area_Almacen { get; set; } = null!;
        public bool Sincro_Document { get; set; }
        public int renglon { get; set; } = 0;
        [JsonIgnore]
        public Equipo? Equipo { get; set; }
        public ICollection<OrderFisicoDetails> OrdersDetails { get; set; } = new List<OrderFisicoDetails>();
    }
}
