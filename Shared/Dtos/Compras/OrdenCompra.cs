using System.ComponentModel.DataAnnotations;

namespace Shared.Dtos.Compras
{
    public class OrdenCompra
    {
        [Key]
        public string Numero { get; set; } = ""; //PK
        public DateTime Fecha { get; set; }
        public string Description { get; set; } = "";
        public int Status { get; set; }
        public bool Sincro { get; set; }
        public double Total { get; set; }
        public ICollection<DetalleCompras> Items { get; set; } = [];
    }
}
