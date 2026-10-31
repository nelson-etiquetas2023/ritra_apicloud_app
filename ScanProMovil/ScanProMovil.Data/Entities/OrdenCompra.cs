using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.CompilerServices;
using Microsoft.Maui.Graphics;

namespace ScanProMovil.Data.Entities
{
    public class OrdenCompra : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

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
                    0 => "Inicializado",
                    1 => "Actualizado",
                    2 => "Por recibir",
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

        //Seleccion multiple en UI (no se persiste en BD).
        private bool _isSelected;
        [NotMapped]
        public bool IsSelected
        {
            get => _isSelected;
            set { if (_isSelected != value) { _isSelected = value; OnPropertyChanged(); } }
        }

        //Puede recibirse solo si esta sincronizada (Status 2) y no procesada (4) ni anulada (6).
        [NotMapped]
        public bool PuedeRecibir => Status == 2;

    }
}
