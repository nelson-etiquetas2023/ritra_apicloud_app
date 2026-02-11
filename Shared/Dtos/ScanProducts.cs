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
        public string ProductName { get; set; } = null!;
        public int Quantity { get; set; } = 0;
        public DateTime DateScan { get; set; }
        public string Ubicacion { get; set; } = null!;
        public string Estado { get; set; } = null!;
        public string Unidad { get; set; } = null!;
        public string Category { get; set; } = null!;
        public string OrdenId { get; set; } = null!;
        public string StateData { get; set; } = "";
    }
}
