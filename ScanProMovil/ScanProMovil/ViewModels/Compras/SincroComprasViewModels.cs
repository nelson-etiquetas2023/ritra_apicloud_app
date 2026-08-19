using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Graphics;
using ScanProMovil.Data.Entities;
using ScanProMovil.Services.Compras;

namespace ScanProMovil.ViewModels.Compras
{
    public partial class SincroComprasViewModels : ObservableObject
    {
        [ObservableProperty]
        public OrdenCompra orden = new();

        [ObservableProperty]
        private bool isSincronizando;

        public bool IsSincronizarEnabled => !IsSincronizando && Orden.Status != 2;

        partial void OnIsSincronizandoChanged(bool value)
        {
            OnPropertyChanged(nameof(IsSincronizarEnabled));
        }

        partial void OnOrdenChanged(OrdenCompra value)
        {
            OnPropertyChanged(nameof(IsSincronizarEnabled));
        }

        [ObservableProperty]
        private string resultadoSincro = string.Empty;

        [ObservableProperty]
        private Color resultadoColor = Colors.LightGray;

        [ObservableProperty]
        private string duracionSincro = string.Empty;

        public IComprasService Service { get; set; }

        public SincroComprasViewModels(IComprasService service, OrdenCompra orden)
        {
            this.Service = service;
            Orden = orden;
        }

        [RelayCommand]
        public async Task SendPurchaseOrderAsync()
        {
            if (Orden.Status == 2)
            {
                ResultadoSincro = "El documento ya fue sincronizado.";
                ResultadoColor = Colors.OrangeRed;
                return;
            }

            IsSincronizando = true;
            ResultadoSincro = "Sincronizando...";
            ResultadoColor = Colors.Blue;
            DuracionSincro = string.Empty;

            // medir la duración de la operación.
            var stopwatch = Stopwatch.StartNew();

            try
            {
                //completar la orden para el envio por la API.
                
                orden.Supply_Id = 0;
                orden.Impuesto = 0;
                orden.Reference = "doc. soncro app movil";
                orden.Tipo_Documento = "compra";

                var ok = await Service.SendPurchaseOrder(Orden);

                if (ok)
                {
                    Orden.Sincro = true;
                    Orden.Status = 2;
                    await Service.MarkOrderSynchronizedAsync(Orden.Numero);

                    ResultadoSincro = "Sincronización exitosa.";
                    ResultadoColor = Colors.Green;
                    Debug.WriteLine($"Sincronización exitosa en {stopwatch.Elapsed.TotalSeconds:F2} s.");
                }
            }
            catch (Exception ex)
            {
                ResultadoSincro = $"Error: {ex.Message}";
                ResultadoColor = Colors.Red;
                Debug.WriteLine($"Error en la sincronización en {stopwatch.Elapsed.TotalSeconds:F2} s.: {ex}");
            }
            finally
            {
                stopwatch.Stop();
                IsSincronizando = false;
                DuracionSincro = $"Duración: {stopwatch.Elapsed.TotalSeconds:F2} s.";
            }
        }
    }
}