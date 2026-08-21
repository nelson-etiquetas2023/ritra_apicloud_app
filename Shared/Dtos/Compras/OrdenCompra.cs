using System.ComponentModel.DataAnnotations;

namespace Shared.Dtos.Compras
{
    public class OrdenCompra
    {
        [Key]
        public string Numero { get; set; } = ""; //PK
        public DateTime Fecha { get; set; }
        public DateTime FechaCreacion { get; set; }
        public string Description { get; set; } = "";
        public string Comentario { get; set; } = "";
        public int Status { get; set; }
        public bool Sincro { get; set; }
        public string Tipo_Documento { get; set; } = "";
        public double Subtotal{ get; set; }
        public double Impuesto { get; set; }
        public double Total { get; set; }
        public int Supply_Id { get; set; }
        public string Supply_Name { get; set; } = "";
        public string Reference { get; set; } = "";
        public string Prioridad { get; set; } = "Normal";
        public string UserName { get; set; } = "";
        public string UserEmail { get; set; } = "";
        public string UserRole { get; set; } = "";
        public string DeviceCode { get; set; } = "";
        public string DeviceName { get; set; } = "";
        public string WarehouseName { get; set; } = "";
        public ICollection<DetalleCompras> Items { get; set; } = [];

        public string StatusTexto
        {
            get
            {
                return Status switch
                {
                    0 => "Publicado-Iniciado",
                    1 => "Modificado",
                    2 => "Sincronizado",
                    3 => "Cerrado",
                    4 => "Procesado",
                    5 => "Transacción Fallida",
                    _ => "Desconocido"
                };
            }
        }
    }
}
