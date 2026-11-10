using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.CompilerServices;
using System.Linq;

namespace ScanProMovil.Data.Entities
{
    /// <summary>
    /// Orden de compra local (movil). Replica el diseño de PickingOrder: los
    /// estados notifican cambios para que la UI refresque en tiempo real.
    /// Status: 1 Iniciado, 2 Por recibir, 3 Recibido, 4 Ubicado.
    /// </summary>
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

        private int _status;
        /// <summary>1 Iniciado, 2 Por recibir, 3 Recibido, 4 Ubicado.</summary>
        public int Status
        {
            get => _status;
            set
            {
                if (_status != value)
                {
                    _status = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(StatusTexto));
                    OnPropertyChanged(nameof(PuedeRecibir));
                }
            }
        }
        public int ItemsNumber { get; set; } = 0;
        public string UserName { get; set; } = "";
        public string UserEmail { get; set; } = "";
        public string UserRole { get; set; } = "";
        public string DeviceCode { get; set; } = "";
        public string DeviceName { get; set; } = "";
        public string WarehouseName { get; set; } = "";
        public ObservableCollection<DetalleCompra> Items { get; set; } = [];

        public string StatusTexto =>
            Status switch
            {
                1 => "Iniciado",
                2 => "Por recibir",
                3 => "Recibido",
                4 => "Ubicado",
                _ => "Desconocido"
            };

        // Puede recibir escaneos mientras siga Por recibir (2).
        public bool PuedeRecibir => Status == 2;

        //Seleccion multiple en UI (no se persiste en BD).
        private bool _isSelected;
        [NotMapped]
        public bool IsSelected
        {
            get => _isSelected;
            set { if (_isSelected != value) { _isSelected = value; OnPropertyChanged(); } }
        }

        /// <summary>Total de unidades por recibir (suma de cantidades de todas las lineas).</summary>
        public int TotalUnidades => Items.Sum(i => (int)Math.Ceiling(i.Cantidad));

        /// <summary>Unidades ya recibidas (suma de lo recibido de todas las lineas).</summary>
        public int UnidadesRecibidas => Items.Sum(i => (int)Math.Floor(Math.Min(i.CantidadRecibida, i.Cantidad)));

        /// <summary>
        /// Líneas completamente recibidas frente al total de líneas, para las
        /// tarjetas del listado (por ejemplo: "0/2 líneas").
        /// </summary>
        public string ProgresoTexto =>
            $"{Items.Count(i => i.RecibidoCompleto)}/{Items.Count} líneas";

        /// <summary>Texto tipo "3 de 5 unidades recibidas".</summary>
        public string ProgresoDetalleTexto =>
            $"{UnidadesRecibidas} de {TotalUnidades} unidades recibidas";

        /// <summary>
        /// Refresca los contadores de progreso que ve el listado (tarjetas)
        /// despues de recibir items o cerrar la orden.
        /// </summary>
        public void NotificarProgresoRecepcion()
        {
            OnPropertyChanged(nameof(TotalUnidades));
            OnPropertyChanged(nameof(UnidadesRecibidas));
            OnPropertyChanged(nameof(ProgresoTexto));
            OnPropertyChanged(nameof(ProgresoDetalleTexto));
            OnPropertyChanged(nameof(Items));
            OnPropertyChanged(nameof(ItemsNumber));
        }
    }
}
