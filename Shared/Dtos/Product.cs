using System.ComponentModel.DataAnnotations;

namespace Shared.Dtos
{
    public class Product
    {
        [Key]
        [Required(ErrorMessage = "el producto debe tener un id...")]
        public int Product_id { get; set; }

        [Required(ErrorMessage = "el producto debe tener un codigo que lo identifique...")]
        public string Product_Code { get; set; } = "";

        [Required(ErrorMessage = "el producto debe tener nombre descriptivo...")]
        public string Product_Name { get; set; } = null!;

        [Required(ErrorMessage ="el producto debe tener una categoria asignada...")]
        public string Product_Type { get; set; } = null!;

        public double Price { get; set; } = 0;

        public string Codebar { get; set; } = null!;

        [Required(ErrorMessage ="el producto debe tener una unidad")]
        public string Unidad { get; set; } = null!;
        public List<ProductImage> Images { get; set; } = [];

        public decimal Costo { get; set; } = 0;
        public double Stock { get; set; } = 0;
        public double Stock_Mix { get; set; }
        public double Stock_Max { get; set; }
        public string StockStatus { get; set; } = null!;
        public string Description { get; set; } = null!;
        public string SkuNumber { get; set; } = null!;
        public string PartNumber { get; set; } = null!;
        public string Model { get; set; } = null!;
        public string Marca { get; set; } = null!;
        public string StatusProducts { get; set; } = null!;
    }
}
