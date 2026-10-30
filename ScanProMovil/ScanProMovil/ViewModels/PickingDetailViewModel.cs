using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Maui.Alerts;
using Microsoft.Maui.Graphics;
using ScanProMovil.Data.Entities;
using ScanProMovil.Services.Picking;
using System.Collections.ObjectModel;
using System.Linq;

namespace ScanProMovil.ViewModels
{
    public partial class PickingDetailViewModel : ObservableObject
    {
        private readonly IPickingService _pickingService;

        [ObservableProperty]
        private bool isLoading;

        [ObservableProperty]
        private string loadingMessage = string.Empty;

        [ObservableProperty]
        private string codigoBarraTexto = string.Empty;

        [ObservableProperty]
        private bool estaBloqueado;

        /// <summary>Mensaje de feedback del escaneo (producto escaneado, no encontrado, error...).</summary>
        [ObservableProperty]
        private string mensajeEscaneo = string.Empty;

        /// <summary>Color del mensaje de feedback (verde OK, rojo error).</summary>
        [ObservableProperty]
        private Color colorMensajeEscaneo = Colors.Transparent;

        public PickingOrder? Orden { get; private set; }

        private ObservableCollection<PickingOrderDetail> _itemsOriginal = [];

        [ObservableProperty]
        private ObservableCollection<PickingOrderDetail> items = [];

        public PickingDetailViewModel(IPickingService pickingService)
        {
            _pickingService = pickingService;
        }

        public void Inicializar(PickingOrder orden)
        {
            Orden = orden;
            EstaBloqueado = orden.Estado == 3;
            _itemsOriginal = new ObservableCollection<PickingOrderDetail>(
                orden.Items.OrderBy(i => i.ProductCode));
            Items = new ObservableCollection<PickingOrderDetail>(_itemsOriginal);

            // Red de seguridad: los escaneos ya se guardan uno a uno en la BD
            // local, pero si la app se cerro justo despues del ultimo escaneo
            // y antes de cerrar la orden, aqui quedara 3/3 sin cerrar. Al
            // reabrir el detalle se cierra ahora (remoto + local) para que
            // la web quede End Picked.
            if (!EstaBloqueado && orden.Estado != 2 &&
                _itemsOriginal.Count != 0 && _itemsOriginal.All(i => i.Completado))
            {
                _ = CierreRecuperadoAsync();
            }
        }

        /// <summary>Cierre que dispara 3/3 pendiente al reabrir el detalle tras haber salido de la app.</summary>
        private async Task CierreRecuperadoAsync()
        {
            IsLoading = true;
            LoadingMessage = "Cerrando orden...";
            try
            {
                await EjecutarCierreOrdenAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PickingDetail] Error en cierre recuperado: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
                LoadingMessage = string.Empty;
            }
        }

        /// <summary>
        /// Procesa un escaneo de código de barras: busca el producto en los ítems pendientes
        /// de la orden, lo marca como completado y verifica si la orden ya terminó.
        /// </summary>
        public async Task ProcesarEscaneoAsync(string codigo)
        {
            if (Orden is null || IsLoading || EstaBloqueado || string.IsNullOrWhiteSpace(codigo)) return;

            try
            {
                IsLoading = true;
                LoadingMessage = "Procesando escaneo...";

                var codigoLimpio = codigo.Trim();

                // Limpiar asteriscos si el escáner Zebra los envía: *CODIGO*
                if (codigoLimpio.Length >= 2 && codigoLimpio.StartsWith('*') && codigoLimpio.EndsWith('*'))
                {
                    codigoLimpio = codigoLimpio.Trim('*', ' ', '\r', '\n', '\t').Trim();
                }

                Console.WriteLine($"[PickingDetail] Código recibido crudo: '{codigo}'");
                Console.WriteLine($"[PickingDetail] Código limpio: '{codigoLimpio}'");
                Console.WriteLine($"[PickingDetail] Total ítems en orden: {_itemsOriginal.Count}");

                foreach (var itm in _itemsOriginal)
                {
                    Console.WriteLine($"[PickingDetail] Ítem: ProductCode='{itm.ProductCode}', ProductName='{itm.ProductName}', Completado={itm.Completado}");
                }

                // 1) Intento coincidencia exacta (case-insensitive)
                var item = _itemsOriginal.FirstOrDefault(i =>
                    !i.Completado &&
                    !string.IsNullOrWhiteSpace(i.ProductCode) &&
                    i.ProductCode.Equals(codigoLimpio, System.StringComparison.OrdinalIgnoreCase));

                // 2) Intento Contains (código contenido en el escaneo o viceversa)
                if (item == null)
                {
                    item = _itemsOriginal.FirstOrDefault(i =>
                        !i.Completado &&
                        !string.IsNullOrWhiteSpace(i.ProductCode) &&
                        (i.ProductCode.Contains(codigoLimpio, System.StringComparison.OrdinalIgnoreCase) ||
                         codigoLimpio.Contains(i.ProductCode, System.StringComparison.OrdinalIgnoreCase)));
                }

                // 3) Fallback: buscar por nombre con Contains
                if (item == null)
                {
                    item = _itemsOriginal.FirstOrDefault(i =>
                        !i.Completado &&
                        !string.IsNullOrWhiteSpace(i.ProductName) &&
                        i.ProductName.Contains(codigoLimpio, System.StringComparison.OrdinalIgnoreCase));
                }

                if (item == null)
                {
                    MensajeEscaneo = "Producto no encontrado en esta orden.";
                    ColorMensajeEscaneo = Colors.Red;
                    var toast = Toast.Make("Producto no encontrado en esta orden.", CommunityToolkit.Maui.Core.ToastDuration.Short);
                    await toast.Show();
                    return;
                }

                // Avance optimista en pantalla: un escaneo pickea una unidad.
                // El servicio guarda en la BD; si falla, se revierte.
                var pickeadoPrevio = item.Pickeado;
                item.Pickeado = Math.Min(item.Pickeado + 1, item.Cantidad);

                // Persistir en la base local (aunque no haya red)
                var ok = await _pickingService.MarcarItemCompletadoAsync(Orden.PickingId, item.DetailId);
                if (!ok)
                {
                    item.Pickeado = pickeadoPrevio; // revertir si falló
                    MensajeEscaneo = "Error al marcar el producto.";
                    ColorMensajeEscaneo = Colors.Red;
                    var toast = Toast.Make("No se pudo marcar el producto.", CommunityToolkit.Maui.Core.ToastDuration.Short);
                    await toast.Show();
                    return;
                }

                // Éxito: feedback visual
                MensajeEscaneo = $"Escaneado: {item.ProductCode}";
                ColorMensajeEscaneo = Colors.Green;

                // El listado de picking (misma instancia en memoria) ve el avance en tiempo real.
                Orden.NotificarProgreso();

                OnPropertyChanged(nameof(Progreso));
                OnPropertyChanged(nameof(CompletadosCount));
                OnPropertyChanged(nameof(PendientesCount));

                // Verificar si todos los ítems de la orden están completados
                var todosCompletados = _itemsOriginal.All(i => i.Completado);
                if (todosCompletados)
                {
                    MensajeEscaneo = "Todos los productos escaneados. Cerrando orden...";
                    ColorMensajeEscaneo = Colors.Green;

                    // Nucleo directo (sin guardias): IsLoading ya esta activo aqui,
                    // si llamara CompletarOrdenAsync la guardia abortaria el cierre.
                    await EjecutarCierreOrdenAsync();
                }
            }
            catch (Exception ex)
            {
                MensajeEscaneo = $"Error: {ex.Message}";
                ColorMensajeEscaneo = Colors.Red;
                var toast = Toast.Make($"Error: {ex.Message}", CommunityToolkit.Maui.Core.ToastDuration.Short);
                await toast.Show();
            }
            finally
            {
                IsLoading = false;
                LoadingMessage = string.Empty;
            }
        }

        /// <summary>
        /// Acción opcional que se invoca al completar la orden (ej. navegar atrás).
        /// Se asigna desde la página para no acoplar el ViewModel a la navegación.
        /// </summary>
        public Action? OnCompletado { get; set; }

        /// <summary>Punto de entrada de UI: aplica guardias y control de carga.</summary>
        [RelayCommand]
        public async Task CompletarOrdenAsync()
        {
            if (Orden is null || IsLoading || EstaBloqueado) return;

            try
            {
                IsLoading = true;
                LoadingMessage = "Completando orden...";

                await EjecutarCierreOrdenAsync();
            }
            catch (Exception ex)
            {
                var toast = Toast.Make($"Error: {ex.Message}", CommunityToolkit.Maui.Core.ToastDuration.Short);
                await toast.Show();
            }
            finally
            {
                IsLoading = false;
                LoadingMessage = string.Empty;
            }
        }

        /// <summary>
        /// Nucleo del cierre: servidor + local + estado 3 + navegacion.
        /// Sin guardias de IsLoading porque tambien lo invoca el escaneo
        /// automatico, que ya trae la carga activa.
        /// </summary>
        private async Task EjecutarCierreOrdenAsync()
        {
            if (Orden is null) return;

            // Guardia de reentrada: si ya esta cerrada, no repetir.
            if (Orden.Estado >= 2 || EstaBloqueado) return;

            // 1) Intentar completar en el servidor (si hay conexión)
            var remotoOk = await _pickingService.CompletarOrdenRemotoAsync(Orden!.PickingId);

            // 2) Marcar localmente (offline-first: se guarda aunque el servidor falle)
            var localOk = await _pickingService.MarcarCompletadoAsync(Orden.PickingId);

            if (localOk)
            {
                Orden.Estado = 3; // End Picked
                Orden.FechaCompletado = DateTime.Now;
                Orden.NotificarProgreso();
                EstaBloqueado = true;
                OnPropertyChanged(nameof(Progreso));
                OnCompletado?.Invoke();

                var toast = remotoOk
                    ? Toast.Make("Orden cerrada ✓", CommunityToolkit.Maui.Core.ToastDuration.Short)
                    : Toast.Make("Orden completada localmente. Se sincronizará al recuperar conexión.", CommunityToolkit.Maui.Core.ToastDuration.Long);
                await toast.Show();
            }
            else
            {
                var toast = Toast.Make("No se pudo completar la orden.", CommunityToolkit.Maui.Core.ToastDuration.Short);
                await toast.Show();
            }
        }

        public int PendientesCount => Items.Count(i => !i.Completado);
        public int CompletadosCount => Items.Count(i => i.Completado);

        /// <summary>Progreso de 0 a 1 para el ProgressBar.</summary>
        public double Progreso => Items.Count == 0 ? 0 : (double)CompletadosCount / Items.Count;
    }
}
