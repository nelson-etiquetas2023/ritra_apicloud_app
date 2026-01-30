
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shared.Dtos
{
    public class OrderFisicoDetails
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        public string OrderNumberID { get; set; } = null!;
        public int Renglon_Id { get; set; }
        public int Product_id { get; set; }
        public string Product_name { get; set; } = null!;
        public string Product_Type { get; set; } = null!;
        public string Roll_Id { get; set; } = null!;
        public string Code_Unique { get; set; } = null!;
        public double Width_Fisico { get; set; }
        public double Length_Fisico { get; set; }
        public double Width_Sistema { get; set; }
        public double Length_Sistema { get; set; }
        public double Width_Dif { get; set; }
        public double Length_Dif { get; set; }
        public string Product_Estado { get; set; } = null!;
        public string Ubicacion { get; set; } = null!;
        public string Notes { get; set; } = null!;
        [JsonIgnore]
        public OrderFisicoHeader? Order { get; set; }
    }
}
