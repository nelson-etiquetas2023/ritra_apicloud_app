using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Maui.Alerts;
using ScanProMovil.Data.Entities;
using ScanProMovil.Services.Picking;
using System.Collections.ObjectModel;

namespace ScanProMovil.ViewModels
{
    public partial class PickingDetailViewModel : ObservableObject
    {
        private readonly IPickingService _pickingService;

        [ObservableProperty]
        private bool isLoading;

        [ObservableProperty]
        private string loadingMessage = string.Empty;

        public PickingOrder? Orden { get; private set; }

        [ObservableProperty]
        private ObservableCollection<PickingOrderDetail> items = [];

        public PickingDetailViewModel(IPickingService pickingService)
        {
            _pickingService = pickingService;
        }

        public void Inicializar(PickingOrder orden)
        {
            Orden = orden;
            Items = new ObservableCollection<PickingOrderDetail>(
                orden.Items.OrderBy(i => i.ProductCode));
        }

        /// <summary>
        /// Acción opcional que se invoca al completar la orden (ej. navegar atrás).
        /// Se asigna desde la página para no acoplar el ViewModel a la navegación.
        /// </summary>
        public Action? OnCompletado { get; set; }

        [RelayCommand]
        public async Task ToggleItemAsync(PickingOrderDetail item)
        {
            if (Orden is null || IsLoading) return;

            try
            {
                IsLoading = true;
                loadingMessage = "Actualizando ítem...";

                var ok = await _pickingService.MarcarItemCompletadoAsync(Orden.PickingId, item.DetailId);
                if (ok)
                {
                    item.Completado = true;
                    OnPropertyChanged(nameof(Progreso));
                    OnPropertyChanged(nameof(CompletadosCount));
                    OnPropertyChanged(nameof(PendientesCount));
                    var pendientes = Items.Count(i => !i.Completado);
                    loadingMessage = $"Quedan {pendientes} producto(s) pendiente(s).";
                }
                else
                {
                    var toast = Toast.Make("No se pudo actualizar el ítem.", CommunityToolkit.Maui.Core.ToastDuration.Short);
                    await toast.Show();
                }
            }
            catch (Exception ex)
            {
                var toast = Toast.Make($"Error: {ex.Message}", CommunityToolkit.Maui.Core.ToastDuration.Short);
                await toast.Show();
            }
            finally
            {
                IsLoading = false;
                loadingMessage = string.Empty;
            }
        }

        [RelayCommand]
        public async Task CompletarOrdenAsync()
        {
            if (Orden is null || IsLoading) return;

            try
            {
                IsLoading = true;
                loadingMessage = "Completando orden...";

                // 1) Intentar completar en el servidor (si hay conexión)
                var remotoOk = await _pickingService.CompletarOrdenRemotoAsync(Orden.PickingId);

                // 2) Marcar localmente (offline-first: se guarda aunque el servidor falle)
                var localOk = await _pickingService.MarcarCompletadoAsync(Orden.PickingId);

                if (localOk)
                {
                    Orden.Estado = 2;
                    Orden.FechaCompletado = DateTime.Now;
                    OnPropertyChanged(nameof(Progreso));
                    OnCompletado?.Invoke();

                    if (!remotoOk)
                    {
                        var toast = Toast.Make("Orden completada localmente. Se sincronizará al recuperar conexión.", CommunityToolkit.Maui.Core.ToastDuration.Long);
                        await toast.Show();
                    }
                }
                else
                {
                    var toast = Toast.Make("No se pudo completar la orden.", CommunityToolkit.Maui.Core.ToastDuration.Short);
                    await toast.Show();
                }
            }
            catch (Exception ex)
            {
                var toast = Toast.Make($"Error: {ex.Message}", CommunityToolkit.Maui.Core.ToastDuration.Short);
                await toast.Show();
            }
            finally
            {
                IsLoading = false;
                loadingMessage = string.Empty;
            }
        }

        public int PendientesCount => Items.Count(i => !i.Completado);
        public int CompletadosCount => Items.Count(i => i.Completado);

        /// <summary>Progreso de 0 a 1 para el ProgressBar.</summary>
        public double Progreso => Items.Count == 0 ? 0 : (double)CompletadosCount / Items.Count;
    }
}
