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
        public DateTime? FechaVencimiento { get; set; }
        public bool ManejaVencimiento { get; set; }
        public bool ManejaSeriales { get; set; }
        public bool ManejaLotes { get; set; }
        public string? StockStatus { get; set; } = "";
        public string? Description { get; set; } = "";
        public string? SkuNumber { get; set; } = "";
        public string? PartNumber { get; set; } = "";
        public string? Model { get; set; } = "";
        public string? Marca { get; set; } = "";
        public string? StatusProducts { get; set; } = "";
        public bool IsActive { get; set; } = true;
    }
}
