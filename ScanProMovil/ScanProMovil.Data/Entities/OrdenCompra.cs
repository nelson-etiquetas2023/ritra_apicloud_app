using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using Microsoft.Maui.Graphics;

namespace ScanProMovil.Data.Entities
{
    public class OrdenCompra
    {
        [Key]
        public string Numero { get; set; } = null!;
        public int OrderId { get; set; }
        public DateTime Fecha { get; set; }
        public DateTime FechaCreacion { get; set; }
        public string Description { get; set; } = "";
        public string Comentario { get; set; } = "";
        public bool Sincro { get; set; }
        public string Tipo_Documento { get; set; } = "";
        public double Subtotal { get; set; }
        public double Impuesto { get; set; }
        public double Total { get; set; }
        public int Supply_Id { get; set; }
        public string Supply_Name { get; set; } = "";
        public string Reference { get; set; } = "";
        public int Status { get; set; }
        public int ItemsNumber { get; set; } = 0;
        public string UserName { get; set; } = "";
        public string UserEmail { get; set; } = "";
        public string UserRole { get; set; } = "";
        public string DeviceCode { get; set; } = "";
        public string DeviceName { get; set; } = "";
        public string WarehouseName { get; set; } = "";
        public ObservableCollection<DetalleCompra> Items { get; set; } = [];

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
                    _ => "Desconocido"
                };
            }
        }
        public Color BorderColor =>
          Status switch
          {
              0 => Colors.LightGreen,
              2 => Colors.OrangeRed,
              _ => Colors.Gold
          };

    }
}
