using System.Diagnostics;
using System.Runtime.CompilerServices;
using ScanProMovil.Services.Logging;

namespace ScanProMovil.Views
{
    /// <summary>
    /// Envoltorio seguro para handlers 'async void' de la UI (OnClick, OnAppearing, etc.).
    /// Una excepción no atrapada en un async void crashea la aplicación en Android;
    /// aquí se registra en el log y se muestra al usuario en lugar de cerrar la app.
    /// </summary>
    public static class UiGuard
    {
        public static async void Run(Element? element, Func<Task> action, [CallerMemberName] string? contexto = null)
        {
            try
            {
                await action();
            }
            catch (Exception ex)
            {
                AppLog.Error($"UI:{contexto}", ex);

                try
                {
                    var target = element as Page ?? Application.Current?.Windows.FirstOrDefault()?.Page;
                    if (target is not null)
                    {
                        await MainThread.InvokeOnMainThreadAsync(async () =>
                            await target.DisplayAlertAsync(
                                "Error inesperado",
                                $"La operación '{contexto}' falló. El detalle quedó registrado en el log.\n\n{ex.Message}",
                                "OK"));
                    }
                }
                catch (Exception exAlert)
                {
                    // Ni siquiera el alerta debe reventar: ya quedó en el log.
                    AppLog.Error("UI:AlertFallback", exAlert);
                }
            }
        }
    }
}
