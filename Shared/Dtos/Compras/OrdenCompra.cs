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
        public string Tipo_Documento { get; set; } = "";
        public double Subtotal{ get; set; }
        public double Impuesto { get; set; }
        public double Total { get; set; }
        public int Supply_Id { get; set; }
        public string Supply_Name { get; set; } = "";
        public string Reference { get; set; } = "";
        public ICollection<DetalleCompras> Items { get; set; } = [];
    }
}
