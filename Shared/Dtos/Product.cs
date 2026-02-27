using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace Shared.Dtos
{
    public class Product
    {
        [Key]
        [Required(ErrorMessage = "el producto debe tener un id...")]
        public int Product_id { get; set; }
        [Required(ErrorMessage = "el producto debe tener nombre descriptivo...")]
        public string Product_Name { get; set; } = null!;
        public string Product_Type { get; set; } = null!;
        public double Price { get; set; } = 0;
        public string Codebar { get; set; } = null!;
    }
}
