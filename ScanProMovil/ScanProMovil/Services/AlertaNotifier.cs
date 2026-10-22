using CommunityToolkit.Maui.Alerts;

namespace ScanProMovil.Services
{
    /// <summary>
    /// Implementacion de los avisos con el Toast de MAUI.
    /// </summary>
    public class AlertaNotifier : IAlertaNotifier
    {
        public async Task MostrarAsync(string mensaje, TipoAlerta tipo = TipoAlerta.Info)
        {
            var duracion = tipo == TipoAlerta.Error
                ? CommunityToolkit.Maui.Core.ToastDuration.Long
                : CommunityToolkit.Maui.Core.ToastDuration.Short;

            var toast = Toast.Make(mensaje, duracion);
            await toast.Show();
        }
    }
}
