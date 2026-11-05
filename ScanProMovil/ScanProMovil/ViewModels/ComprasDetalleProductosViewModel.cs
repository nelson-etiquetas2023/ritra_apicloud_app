
using System;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Graphics;
using ScanProMovil.Data.Entities;
using ScanProMovil.Services.Compras;
using System.Collections.ObjectModel;
using System.Linq;

namespace ScanProMovil.ViewModels
{
    /// <summary>Detalle de recepcion de una orden de compra (replica el diseño de
    /// PickingDetailViewModel): escaneo pasivo optimista con rollback, guardado
    /// uno a uno en SQLite, cierre automatico al completar y cierre recuperado
    /// al reabrir el detalle.</summary>
    public partial class ComprasDetalleProductosViewModel : ObservableObject
    {
        private readonly IComprasService _comprasService;

        [ObservableProperty]
        private bool isLoading;

        [ObservableProperty]
        private string loadingMessage = string.Empty;

        [ObservableProperty]
        private bool estaBloqueado;

        /// <summary>Mensaje de feedback del escaneo (producto escaneado, no encontrado, error...).</summary>
        [ObservableProperty]
        private string mensajeEscaneo = string.Empty;

        /// <summary>Color del mensaje de feedback (verde escaneo, rojo error, naranja advertencia).</summary>
        [ObservableProperty]
        private Color colorMensajeEscaneo = Colors.Transparent;

        public OrdenCompra? Orden { get; private set; }

        private ObservableCollection<DetalleCompra> _itemsOriginal = [];

        [ObservableProperty]
        private ObservableCollection<DetalleCompra> items = [];

        public bool TodosRecibidos => Items.Count > 0 && Items.All(i => i.RecibidoCompleto);

        public string ProgresoTexto => $"{Items.Count(i => i.RecibidoCompleto)}/{Items.Count}";

        public double Progreso => Items.Count == 0 ? 0 : (double)Items.Count(i => i.RecibidoCompleto) / Items.Count;

        /// <summary>Accion opcional al completar la orden (ej. navegar atras).
        /// Se asigna desde la pagina para no acoplar el ViewModel a la navegacion.</summary>
        public Action? OnCompletado { get; set; }

        public ComprasDetalleProductosViewModel(IComprasService comprasService)
        {
            _comprasService = comprasService;
        }

        public void Inicializar(OrdenCompra orden)
        {
            Orden = orden;

            // Bloquea el escaneo si la orden ya no acepta recepciones
            // (3 Cerrado, 4 Procesado, 6 Anulado). Con 5 (Transaccion Fallida)
            // el servidor deja continuar, igual que con 2 (Por recibir).
            EstaBloqueado = !orden.PuedeRecibir;
            if (EstaBloqueado)
            {
                MensajeEscaneo = "Esta orden ya no acepta recepciones (cerrada, procesada o anulada).";
                ColorMensajeEscaneo = Colors.Orange;
            }

            _itemsOriginal = new ObservableCollection<DetalleCompra>(
                (orden.Items ?? new()).OrderBy(i => i.Product_id));
            Items = new ObservableCollection<DetalleCompra>(_itemsOriginal);
            Recalcular();

            // Red de seguridad: los escaneos ya se guardan uno a uno en la BD
            // local, pero si la app se cerro justo despues del ultimo escaneo
            // y antes de enviar la recepcion, aqui quedara 5/5 sin enviar. Al
            // reabrir el detalle se envia ahora (server + cierre de la orden).
            if (!EstaBloqueado && _itemsOriginal.Count != 0 && _itemsOriginal.All(i => i.RecibidoCompleto))
            {
                _ = CierreRecuperadoAsync();
            }
        }

        /// <summary>Envio de la recepcion que quedo pendiente al reabrir el detalle tras salir de la app.</summary>
        private async Task CierreRecuperadoAsync()
        {
            IsLoading = true;
            LoadingMessage = "Enviando recepcion...";
            try
            {
                await EjecutarCierreOrdenAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ComprasDetalle] Error en cierre recuperado: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
                LoadingMessage = string.Empty;
            }
        }

        private void Recalcular()
        {
            OnPropertyChanged(nameof(Progreso));
            OnPropertyChanged(nameof(ProgresoTexto));
            OnPropertyChanged(nameof(TodosRecibidos));
        }

        /// <summary>
        /// Procesa un escaneo de codigo de barras: busca el producto entre las
        /// lineas pendientes de la orden, suma una unidad recibida y verifica si
        /// la orden ya termino (guardo uno a uno en SQLite, offline-first).
        /// </summary>
        [RelayCommand]
        public async Task ProcesarEscaneo(string codigo)
        {
            if (Orden is null || IsLoading || EstaBloqueado || string.IsNullOrWhiteSpace(codigo)) return;

            try
            {
                IsLoading = true;
                LoadingMessage = "Procesando escaneo...";

                var codigoLimpio = codigo.Trim();

                // Limpiar asteriscos si el escaner Zebra los envia: *CODIGO*
                if (codigoLimpio.Length >= 2 && codigoLimpio.StartsWith('*') && codigoLimpio.EndsWith('*'))
                {
                    codigoLimpio = codigoLimpio.Trim('*', ' ', '\r', '\n', '\t').Trim();
                }

                Console.WriteLine($"[ComprasDetalle] Codigo recibido crudo: '{codigo}'");
                Console.WriteLine($"[ComprasDetalle] Codigo limpio: '{codigoLimpio}'");
                Console.WriteLine($"[ComprasDetalle] Total lineas en orden: {_itemsOriginal.Count}");

                // 1) Intento coincidencia exacta (case-insensitive) entre pendientes
                var item = _itemsOriginal.FirstOrDefault(i =>
                    !i.RecibidoCompleto &&
                    !string.IsNullOrWhiteSpace(i.Product_id) &&
                    i.Product_id.Equals(codigoLimpio, System.StringComparison.OrdinalIgnoreCase));

                // 2) Intento Contains (codigo contenido en el escaneo o viceversa)
                if (item == null)
                {
                    item = _itemsOriginal.FirstOrDefault(i =>
                        !i.RecibidoCompleto &&
                        !string.IsNullOrWhiteSpace(i.Product_id) &&
                        (i.Product_id.Contains(codigoLimpio, System.StringComparison.OrdinalIgnoreCase) ||
                         codigoLimpio.Contains(i.Product_id, System.StringComparison.OrdinalIgnoreCase)));
                }

                // 3) Fallback: buscar por nombre con Contains
                if (item == null)
                {
                    item = _itemsOriginal.FirstOrDefault(i =>
                        !i.RecibidoCompleto &&
                        !string.IsNullOrWhiteSpace(i.Product_Name) &&
                        i.Product_Name.Contains(codigoLimpio, System.StringComparison.OrdinalIgnoreCase));
                }

                if (item == null)
                {
                    // Si la orden ya esta completa, avisarlo de forma clara: el
                    // escaneo no fue "no encontrado", es que la linea ya cerro.
                    var mensaje = TodosRecibidos
                        ? "Todos los productos de esta orden ya fueron recibidos."
                        : "Producto no encontrado en esta orden de compra.";
                    MensajeEscaneo = mensaje;
                    ColorMensajeEscaneo = Colors.Red;
                    var toast = Toast.Make(mensaje, CommunityToolkit.Maui.Core.ToastDuration.Short);
                    await toast.Show();
                    return;
                }

                // Avance optimista en pantalla: un escaneo suma una unidad. El
                // servicio guarda en la BD; si falla, se revierte.
                var recibidoPrevio = item.CantidadRecibida;
                item.CantidadRecibida = Math.Min(item.CantidadRecibida + 1, item.Cantidad);

                // Persistir en la base local (aunque no haya red)
                var ok = await _comprasService.MarcarItemRecibidoAsync(Orden!.Numero, item.DetailId);
                if (!ok)
                {
                    item.CantidadRecibida = recibidoPrevio; // revertir si fallo
                    MensajeEscaneo = "No se pudo guardar la recepción.";
                    ColorMensajeEscaneo = Colors.Red;
                    var toast = Toast.Make("No se pudo guardar la recepción.", CommunityToolkit.Maui.Core.ToastDuration.Short);
                    await toast.Show();
                    return;
                }

                // Exito: feedback visual. El INPC de la entidad refresca la fila;
                // los contadores de la orden (tarjetas del listado, misma instancia
                // en memoria) se refrescan via NotificarProgresoRecepcion.
                MensajeEscaneo = $"{item.Product_id} recibido ({item.RecibidoTexto}).";
                ColorMensajeEscaneo = Colors.Green;

                Orden!.NotificarProgresoRecepcion();
                Recalcular();

                // Verificar si todas las lineas de la orden estan recibidas
                if (_itemsOriginal.All(i => i.RecibidoCompleto))
                {
                    MensajeEscaneo = "Todos los productos recibidos. Enviando recepción...";
                    ColorMensajeEscaneo = Colors.Green;

                    // Nucleo directo (sin guardias): IsLoading ya esta activo aqui,
                    // si llamara ValidarRecepcionCommand la guardia abortaria el cierre.
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

        /// <summary>Punto de entrada de UI (Validar Recepción): aplica guardias y control de carga.</summary>
        [RelayCommand]
        public async Task ValidarRecepcion()
        {
            if (Orden is null || IsLoading || EstaBloqueado) return;

            var unidades = Orden.UnidadesRecibidas;
            if (unidades == 0)
            {
                MensajeEscaneo = "Escanee al menos un producto antes de enviar la recepción.";
                ColorMensajeEscaneo = Colors.Orange;
                return;
            }

            try
            {
                IsLoading = true;
                LoadingMessage = "Enviando recepción...";

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
        /// Nucleo del envio: servidor (delta de recepcion) + cierre de la orden
        /// si quedo procesada. Sin guardias de IsLoading porque tambien lo invoca
        /// el escaneo automatico, que ya trae la carga activa.
        /// </summary>
        private async Task EjecutarCierreOrdenAsync()
        {
            if (Orden is null) return;

            // Guardia de reentrada: si ya cerro, no repetir.
            if (EstaBloqueado) return;

            // 1) Enviar al servidor lo recibido (delta; offline falla y sigue).
            var resultado = await _comprasService.ReceiveOrderAsync(Orden.Numero);

            if (resultado.StatusFinal == 4)
            {
                // El servidor recibio la orden completa: cerrar localmente + UI.
                Orden.Status = 4;
                Orden.Sincro = true;
                EstaBloqueado = true;
                Orden.NotificarProgresoRecepcion();
                Recalcular();
                OnCompletado?.Invoke();

                var toast = Toast.Make("Orden recepcionada ✓", CommunityToolkit.Maui.Core.ToastDuration.Short);
                await toast.Show();
            }
            else if (resultado.StatusFinal == 5)
            {
                // Transaccion Fallida: hay lineas con error; la orden sigue
                // recepcionandose (el servidor lo permite).
                Orden.Status = 5;
                Recalcular();
                MensajeEscaneo = $"Fallo parcial: {resultado.Message}";
                ColorMensajeEscaneo = Colors.Orange;
                var toast = Toast.Make($"Fallo parcial en la recepción. {resultado.Message}", CommunityToolkit.Maui.Core.ToastDuration.Long);
                await toast.Show();
            }
            else if (resultado.Success)
            {
                // Recepcion parcial exitosa: la orden sigue Por Recibir (2) y
                // aqui se queda para seguir escaneando.
                MensajeEscaneo = resultado.Message;
                ColorMensajeEscaneo = Colors.Green;
            }
            else
            {
                MensajeEscaneo = $"Error: {resultado.Message}";
                ColorMensajeEscaneo = Colors.Red;
                var toast = Toast.Make($"Error en la recepción: {resultado.Message}", CommunityToolkit.Maui.Core.ToastDuration.Long);
                await toast.Show();
            }
        }
    }
}
