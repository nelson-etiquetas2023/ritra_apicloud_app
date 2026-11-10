
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

        /// <summary>Progreso de la orden para el header del diálogo: "recibidas / total" (ej. 1/50).</summary>
        public string ProgresoOrdenTexto
        {
            get
            {
                if (Orden is null) return "0/0";
                return $"{Orden.UnidadesRecibidas}/{Orden.TotalUnidades}";
            }
        }

        /// <summary>Código escrito en el entry de escaneo dentro del diálogo.</summary>
        private string _codigoEscaneoDialogo = string.Empty;
        public string CodigoEscaneoDialogo
        {
            get => _codigoEscaneoDialogo;
            set { SetProperty(ref _codigoEscaneoDialogo, value); }
        }

        /// <summary>Accion opcional al completar la orden (ej. navegar atras).
        /// Se asigna desde la pagina para no acoplar el ViewModel a la navegacion.</summary>
        public Action? OnCompletado { get; set; }

        /// <summary>Accion opcional al cerrar el dialogo de confirmacion (devuelve foco al escaner).</summary>
        public Action? OnDialogoCerrado { get; set; }

        /// <summary>Accion opcional al abrir el dialogo de confirmacion (da foco al entry de escaneo).</summary>
        public Action? OnDialogoAbierto { get; set; }

        // --- Diálogo de confirmación de recepción (patrón WMS: scan & confirm) ---
        private DetalleCompra? _productoDialog;
        public DetalleCompra? ProductoDialog
        {
            get => _productoDialog;
            set { SetProperty(ref _productoDialog, value); }
        }

        private int _cantidadDialog = 1;
        public int CantidadDialog
        {
            get => _cantidadDialog;
            set
            {
                if (SetProperty(ref _cantidadDialog, value))
                {
                    // Al cambiar la cantidad (manual o +/-), actualizar el producto,
                    // guardar en SQLite y refrescar el header (RecibidoTexto + progreso).
                    _ = AplicarCantidadDialogoAsync();
                }
            }
        }

        private async Task AplicarCantidadDialogoAsync()
        {
            if (ProductoDialog is null || Orden is null) return;

            // Nunca exceder lo ordenado
            var cantidad = Math.Min(CantidadDialog, (int)ProductoDialog.Cantidad);
            if (cantidad < 0) cantidad = 0;

            if (Math.Abs(cantidad - ProductoDialog.CantidadRecibida) < 0.001) return;

            ProductoDialog.CantidadRecibida = cantidad;

            // Persistir en SQLite (offline-first)
            var ok = await _comprasService.MarcarItemRecibidoAsync(Orden.Numero, ProductoDialog.DetailId);
            if (!ok)
            {
                MensajeEscaneo = "No se pudo guardar la recepción.";
                ColorMensajeEscaneo = Colors.Red;
                return;
            }

            // Notificar progreso de la orden (actualiza el header RecibidoTexto)
            Orden.NotificarProgresoRecepcion();
            Recalcular();

            MensajeEscaneo = $"{ProductoDialog.Product_id} recibido ({ProductoDialog.RecibidoTexto}).";
            ColorMensajeEscaneo = Colors.Green;
        }

        private bool _dialogoAbierto;
        public bool DialogoAbierto
        {
            get => _dialogoAbierto;
            set { SetProperty(ref _dialogoAbierto, value); }
        }

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
            OnPropertyChanged(nameof(ProgresoOrdenTexto));
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

                // WMS: scan & confirm. Abre el diálogo para que el operador
                // valide producto y ajuste la cantidad antes de persistir.
                ProductoDialog = item;
                CantidadDialog = 1;
                DialogoAbierto = true;
                return;
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

        [RelayCommand]
        private void IncrementarDialogo()
        {
            if (ProductoDialog is null) return;
            CantidadDialog = Math.Min(CantidadDialog + 1, (int)ProductoDialog.Cantidad);
        }

        [RelayCommand]
        private void DecrementarDialogo()
        {
            if (CantidadDialog > 1) CantidadDialog--;
        }

        [RelayCommand]
        private async Task GuardarDialogo()
        {
            if (ProductoDialog is null || Orden is null) return;

            // Nunca exceder lo ordenado
            if (CantidadDialog > ProductoDialog!.Cantidad)
                CantidadDialog = (int)ProductoDialog.Cantidad;

            // Aplicar al ítem local
            ProductoDialog.CantidadRecibida = CantidadDialog;

            // Persistir en SQLite (offline-first)
            var ok = await _comprasService.MarcarItemRecibidoAsync(Orden.Numero, ProductoDialog.DetailId);
            if (!ok)
            {
                MensajeEscaneo = "Error al guardar la recepción.";
                ColorMensajeEscaneo = Colors.Red;
                var toast = Toast.Make("No se pudo guardar la recepción.", CommunityToolkit.Maui.Core.ToastDuration.Short);
                await toast.Show();
                return;
            }

            Orden.NotificarProgresoRecepcion();
            Recalcular();

            MensajeEscaneo = $"{ProductoDialog.Product_id} recibido ({CantidadDialog}).";
            ColorMensajeEscaneo = Colors.Green;

            // Cerrar diálogo y devolver foco al escáner para el siguiente producto
            ProductoDialog = null;
            CantidadDialog = 1;
            DialogoAbierto = false;
            OnDialogoCerrado?.Invoke();

            // Verificar si la orden terminó: cierre automático al completar
            if (_itemsOriginal.All(i => i.RecibidoCompleto))
            {
                await EjecutarCierreOrdenAsync();
            }
        }

        [RelayCommand]
        private void CancelarDialogo()
        {
            ProductoDialog = null;
            CantidadDialog = 1;
            DialogoAbierto = false;
        }

        /// <summary>Abre el diálogo de confirmación al tocar un producto en la lista.</summary>
        [RelayCommand]
        private void AbrirDialogo(DetalleCompra item)
        {
            if (Orden is null || EstaBloqueado) return;
            if (item is null || item.RecibidoCompleto) return;

            ProductoDialog = item;
            CantidadDialog = (int)item.CantidadRecibida;
            CodigoEscaneoDialogo = string.Empty;
            DialogoAbierto = true;

            // Notificar a la página para que de el foco al entry de escaneo del diálogo
            OnDialogoAbierto?.Invoke();
        }

        /// <summary>Escanea un código de barras desde dentro del diálogo:
        /// verifica que pertenezca a un producto de la orden y, si es correcto,
        /// selecciona ese producto e incrementa la cantidad. Si no, lanza error.</summary>
        [RelayCommand]
        private async Task ProcesarEscaneoDialogo(string codigo)
        {
            if (Orden is null || EstaBloqueado || string.IsNullOrWhiteSpace(codigo)) return;

            var codigoLimpio = codigo.Trim();
            if (codigoLimpio.Length >= 2 && codigoLimpio.StartsWith('*') && codigoLimpio.EndsWith('*'))
            {
                codigoLimpio = codigoLimpio.Trim('*', ' ', '\r', '\n', '\t').Trim();
            }

            // 1) Coincidencia exacta entre productos NO completados
            var item = _itemsOriginal.FirstOrDefault(i =>
                !i.RecibidoCompleto &&
                !string.IsNullOrWhiteSpace(i.Product_id) &&
                i.Product_id.Equals(codigoLimpio, System.StringComparison.OrdinalIgnoreCase));

            // 2) Intento Contains (código contenido en el escaneo o viceversa)
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
                // Código no válido para esta orden
                MensajeEscaneo = "Código incorrecto";
                ColorMensajeEscaneo = Colors.Red;
                var toast = Toast.Make("Código incorrecto", CommunityToolkit.Maui.Core.ToastDuration.Short);
                await toast.Show();
                CodigoEscaneoDialogo = string.Empty;
                return;
            }

            // Código correcto: seleccionar el producto en el diálogo
            ProductoDialog = item;
            CodigoEscaneoDialogo = string.Empty;

            if (ProductoDialog!.CantidadRecibida >= ProductoDialog.Cantidad)
            {
                // Ya está completo: no se puede incrementar más
                MensajeEscaneo = $"{item.Product_id} ya está completo.";
                ColorMensajeEscaneo = Colors.Orange;
                var toast = Toast.Make($"{item.Product_id} ya está completo.", CommunityToolkit.Maui.Core.ToastDuration.Short);
                await toast.Show();
                return;
            }

            // Avance optimista: incrementar la cantidad recibida inmediatamente (como picking)
            var recibidoPrevio = ProductoDialog.CantidadRecibida;
            ProductoDialog.CantidadRecibida = Math.Min(ProductoDialog.CantidadRecibida + 1, ProductoDialog.Cantidad);

            // Persistir en SQLite (offline-first)
            var ok = await _comprasService.MarcarItemRecibidoAsync(Orden!.Numero, ProductoDialog.DetailId);
            if (!ok)
            {
                ProductoDialog.CantidadRecibida = recibidoPrevio; // revertir si falló
                MensajeEscaneo = "No se pudo guardar la recepción.";
                ColorMensajeEscaneo = Colors.Red;
                var toast = Toast.Make("No se pudo guardar la recepción.", CommunityToolkit.Maui.Core.ToastDuration.Short);
                await toast.Show();
                return;
            }

            // Sincronizar el entry de cantidad con el valor guardado
            CantidadDialog = (int)ProductoDialog.CantidadRecibida;

            // Notificar progreso de la orden (actualiza el header Total items a recibir: N/M)
            Orden.NotificarProgresoRecepcion();
            Recalcular();

            MensajeEscaneo = $"{item.Product_id} recibido ({ProductoDialog.RecibidoTexto}).";
            ColorMensajeEscaneo = Colors.Green;
            var toastOk = Toast.Make($"{item.Product_id} recibido.", CommunityToolkit.Maui.Core.ToastDuration.Short);
            await toastOk.Show();

            // Si este escaneo completó la última línea, cerrar y enviar la
            // recepción automáticamente sin exigir pulsar otro botón.
            await CerrarSiCompletaAsync();
        }

        private async Task CerrarSiCompletaAsync()
        {
            if (Orden is null || EstaBloqueado || _itemsOriginal.Count == 0 ||
                !_itemsOriginal.All(i => i.RecibidoCompleto))
            {
                return;
            }

            ProductoDialog = null;
            CantidadDialog = 1;
            DialogoAbierto = false;
            OnDialogoCerrado?.Invoke();

            await EjecutarCierreOrdenAsync();
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

            var todasLasLineasCompletas = _itemsOriginal.Count > 0 &&
                _itemsOriginal.All(i => i.RecibidoCompleto);

            // 3 es el estado oficial de Recibido. El 4 se acepta únicamente
            // como compatibilidad temporal con una API antigua, pero si todas
            // las cantidades están completas también se cierra localmente.
            if (resultado.StatusFinal == 3 ||
                (resultado.StatusFinal == 4 && todasLasLineasCompletas))
            {
                // El servidor recibio la orden completa: marcarla como recibida.
                Orden.Status = 3;
                Orden.Sincro = true;
                EstaBloqueado = true;
                Orden.NotificarProgresoRecepcion();
                Recalcular();
                OnCompletado?.Invoke();

                var toast = Toast.Make("Orden recepcionada ✓", CommunityToolkit.Maui.Core.ToastDuration.Short);
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
