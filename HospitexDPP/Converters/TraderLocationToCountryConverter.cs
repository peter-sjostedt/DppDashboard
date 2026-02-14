using System.Globalization;
using System.Windows.Data;

namespace HospitexDPP.Converters
{
    /// <summary>
    /// Extracts country from a trader_location address string.
    /// Takes the last segment after the final comma. If no comma, returns the whole string.
    /// </summary>
    public class TraderLocationToCountryConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is not string location || string.IsNullOrWhiteSpace(location))
                return null;

            var lastComma = location.LastIndexOf(',');
            if (lastComma >= 0 && lastComma < location.Length - 1)
                return location[(lastComma + 1)..].Trim();

            return location.Trim();
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
