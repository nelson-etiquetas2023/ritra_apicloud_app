using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using Microsoft.Maui.Graphics;

namespace ScanProMovil.Data.Entities
{
    public class Order
    {
        [Key]
        public int OrderId { get; set; }
        public string OrderNumber { get; set; } = null!;
        public DateTime OrderDate { get; set; }
        public int Status { get; set; }
        public int ItemsNumber { get; set; } = 0;
        public double TotalCosto { get; set; } = 0;
        public string UserName { get; set; } = "";
        public string UserEmail { get; set; } = "";
        public string UserRole { get; set; } = "";
        public string DeviceCode { get; set; } = "";
        public string DeviceName { get; set; } = "";
        public string WarehouseName { get; set; } = "";
        public ObservableCollection<OrderDetails> Items { get; set; } = [];

        public string StatusTexto
        {
            get
            {
                return Status switch
                {
                    0 => "Pendiente",
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
              0 => Colors.OrangeRed,
              2 => Colors.GreenYellow,
              _ => Colors.Gold
          };

    }
}
