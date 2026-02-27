using System.ComponentModel.DataAnnotations;

namespace Shared.Dtos
{
    public class ScanProducts
    {
        [Key]
        public Guid guid { get; set; }
        public string Codebar { get; set; } = null!;
        public bool Selection { get; set; } = false;
        public int Renglon { get; set; }
        [Required(ErrorMessage = "el nombtre del producto debe tener un valor...")]
        public string ProductName { get; set; } = null!;
        [Range(1, int.MaxValue, ErrorMessage = "solo valores mayor que 1...")]
        public int Quantity { get; set; } = 0;
        public DateTime DateScan { get; set; }
        [Required(ErrorMessage ="el producto debe tener un valor de ubicacion en el almacen")]
        public string Ubicacion { get; set; } = null!;
        public string Estado { get; set; } = null!;
        [Required(ErrorMessage = "el producto debe tener unidad...")]
        public string Unidad { get; set; } = null!;
        [Required(ErrorMessage = "el nombtre del producto debe tener una categoria...")]
        public string Category { get; set; } = null!;
        [Required(ErrorMessage ="el producto debe un valor de documento asignado...")]
        public string OrdenId { get; set; } = null!;
        public string StateData { get; set; } = "";
    }
}
