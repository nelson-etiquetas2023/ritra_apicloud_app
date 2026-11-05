using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace ScanProMovil.Data.Entities
{
    /// <summary>Linea de una orden de compra local (movil). Replica el diseño de
    /// PickingOrderDetail: el tracking de recepcion notifica cambios para que la
    /// UI refresque en tiempo real mientras se escanea.</summary>
    public class DetalleCompra : INotifyPropertyChanged
    {
        [JsonIgnore]
        public int DetailId { get; set; }
        public int OrderId { get; set; }
        public string Numero { get; set; } = null!;
        public string Product_id { get; set; } = null!;
        public string Product_Name { get; set; } = null!;
        public double Cantidad { get; set; }
        public double Costo { get; set; } = 0;
        public double Subtotal { get; set; }

        // --- Tracking de recepcion (se persiste en BD para recibir offline-first) ---
        private double _cantidadRecibida;

        /// <summary>Cantidad fisicamente escaneada/recibida de este producto.</summary>
        public double CantidadRecibida
        {
            get => _cantidadRecibida;
            set
            {
                if (_cantidadRecibida != value)
                {
                    _cantidadRecibida = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(RecibidoCompleto));
                    OnPropertyChanged(nameof(CantidadPendiente));
                    OnPropertyChanged(nameof(ProgresoRecepcion));
                    OnPropertyChanged(nameof(RecibidoTexto));
                }
            }
        }

        /// <summary>La linea esta cerrada cuando ya se recibio toda su cantidad (o la
        /// orden no especifica cantidad y ya llego algo).</summary>
        [NotMapped]
        public bool RecibidoCompleto => Cantidad <= 0 ? CantidadRecibida > 0 : CantidadRecibida >= Cantidad;

        /// <summary>Cantidad pendiente por recibir de este producto.</summary>
        [NotMapped]
        public double CantidadPendiente => Math.Max(0, Cantidad - Math.Min(CantidadRecibida, Cantidad));

        /// <summary>Progreso de recepcion (0 a 1).</summary>
        [NotMapped]
        public double ProgresoRecepcion => Cantidad <= 0
            ? (CantidadRecibida > 0 ? 1 : 0)
            : Math.Min(1, CantidadRecibida / Cantidad);

        /// <summary>Texto tipo "2/5" del progreso de la linea.</summary>
        [NotMapped]
        public string RecibidoTexto => $"{Math.Min(CantidadRecibida, Cantidad):0.##}/{Cantidad:0.##}";

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
