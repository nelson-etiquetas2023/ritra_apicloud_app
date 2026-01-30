using System.ComponentModel.DataAnnotations;

namespace Shared.Dtos
{
    public class Product
    {
        [Key]
        public int Product_id { get; set; }
        public string Product_Name { get; set; } = null!;
        public string Product_Type { get; set; } = null!;
    }
}
