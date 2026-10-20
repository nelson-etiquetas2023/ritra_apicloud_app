using System.Globalization;

namespace ScanProMovil.Converters
{
    public class IntToHeightConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is int count && count > 0)
                return count * 40; // cada item ~40 px
            return 80; // altura mínima para 2 items
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
