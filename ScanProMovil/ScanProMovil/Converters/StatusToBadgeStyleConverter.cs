using System.Globalization;

namespace ScanProMovil.Converters
{
    public class StatusToBadgeStyleConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            string status = value?.ToString() ?? string.Empty;


            return status switch
            {
                "Pendiente" => Application.Current?.Resources["BadgePendiente"] as Style,
                "Modificado" => Application.Current?.Resources["BadgeModificado"] as Style,
                "Iniciado" => Application.Current?.Resources["BadgeIniciado"] as Style,
                "Actualizado" => Application.Current?.Resources["BadgeActualizado"] as Style,
                "Sincronizado" => Application.Current?.Resources["BadgeSincro"] as Style,
                "Cerrado" => Application.Current?.Resources["BadgeCerrado"] as Style,
                "Anulado" => Application.Current?.Resources["AnuladoBadgeStyle"] as Style,
                "En Proceso" => Application.Current?.Resources["EnProcesoBadgeStyle"] as Style,
                _ => Application.Current?.Resources["BadgeDefault"] as Style,
            };
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
