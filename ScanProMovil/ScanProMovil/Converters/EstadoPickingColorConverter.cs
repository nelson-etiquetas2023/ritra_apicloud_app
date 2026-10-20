using System.Globalization;
using Microsoft.Maui.Graphics;

namespace ScanProMovil.Converters
{
    public class EstadoPickingColorConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value switch
            {
                int estado when estado == 0 => Colors.Orange,
                int estado when estado == 1 => Colors.Blue,
                int estado when estado == 2 => Colors.Green,
                _ => Colors.Gray
            };
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
