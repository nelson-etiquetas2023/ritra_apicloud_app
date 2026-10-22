namespace ScanProMovil.Services
{
    /// <summary>
    /// Avisos al usuario. Permite que los view models vivan fuera del proyecto
    /// de la app (y se puedan probar sin Android) sin depender de MAUI.
    /// </summary>
    public interface IAlertaNotifier
    {
        Task MostrarAsync(string mensaje, TipoAlerta tipo = TipoAlerta.Info);
    }

    public enum TipoAlerta
    {
        Info,
        Exito,
        Error
    }
}
