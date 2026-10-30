using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace ScanProMovil.Data.Entities
{
    public class PickingOrder : INotifyPropertyChanged
    {
        [Key]
        public int PickingId { get; set; }

        public string NumeroPedidoVenta { get; set; } = null!;

        private int _estado;
        public int Estado
        {
            get => _estado;
            set
            {
                if (_estado != value)
                {
                    _estado = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(EstadoTexto));
                    OnPropertyChanged(nameof(EsPicked));
                }
            }
        } // 0 = Pendiente, 1 = Asignado, 2 = Completado, 3 = End Picked

        public DateTime FechaCreacion { get; set; }

        public string? UsuarioAsignado { get; set; }

        /// <summary>Cliente del pedido, para saber a donde se entrega.</summary>
        public string? ClienteNombre { get; set; }

        public DateTime? FechaCompletado { get; set; }

        /// <summary>true cuando la orden ya fue pickeada y cerrada en el dispositivo (estado 3).</summary>
        public bool EsPicked => Estado == 3;

        public string EstadoTexto =>
            Estado switch
            {
                0 => "Pendiente",
                1 => "Asignado",
                2 => "Completado",
                3 => "End Picked",
                _ => Estado.ToString()
            };

        /// <summary>Total de unidades por pickear (suma de cantidades de todas las lineas).</summary>
        public int TotalItems => Items.Sum(i => i.Cantidad);

        /// <summary>Unidades ya pickeadas (suma de Pickeado de todas las lineas).</summary>
        public int ItemsCompletados => Items.Sum(i => i.Pickeado);

        /// <summary>Texto tipo "2/5".</summary>
        public string ProgresoTexto => $"{ItemsCompletados}/{TotalItems}";

        /// <summary>Texto tipo "2 pickeados de 5".</summary>
        public string ProgresoDetalleTexto => $"{ItemsCompletados} pickeado{(ItemsCompletados != 1 ? "s" : "")} de {TotalItems}";

        public ObservableCollection<PickingOrderDetail> Items { get; set; } = [];

        /// <summary>
        /// Refresca los contadores de progreso que ve el listado (tarjetas)
        /// despues de pickear items o cerrar la orden.
        /// </summary>
        public void NotificarProgreso()
        {
            OnPropertyChanged(nameof(TotalItems));
            OnPropertyChanged(nameof(ItemsCompletados));
            OnPropertyChanged(nameof(ProgresoTexto));
            OnPropertyChanged(nameof(ProgresoDetalleTexto));
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class PickingOrderDetail : INotifyPropertyChanged
    {
        public int DetailId { get; set; }

        public int PickingId { get; set; }

        [JsonIgnore]
        public PickingOrder? PickingOrder { get; set; }

        public string ProductCode { get; set; } = null!;

        public string ProductName { get; set; } = null!;

        public int Cantidad { get; set; }

        /// <summary>Cantidad pickeada de la linea: se incrementa 1 por cada escaneo del mismo codigo.</summary>
        public int Pickeado
        {
            get => _pickeado;
            set
            {
                if (_pickeado != value)
                {
                    _pickeado = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(Completado));
                    OnPropertyChanged(nameof(CantidadPickeada));
                    OnPropertyChanged(nameof(ProgresoTexto));
                }
            }
        }

        private int _pickeado;

        /// <summary>La linea esta cerrada cuando ya se pickeo toda su cantidad.</summary>
        public bool Completado => Cantidad <= 0 ? Pickeado > 0 : Pickeado >= Cantidad;

        /// <summary>Cantidad pickeada (vista de Pickeado para las vistas).</summary>
        public int CantidadPickeada => Pickeado;

        /// <summary>Texto tipo "0/1" o "1/2".</summary>
        public string ProgresoTexto => $"{CantidadPickeada}/{Cantidad}";

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
