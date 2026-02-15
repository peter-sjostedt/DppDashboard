using System.Globalization;
using System.Windows.Data;
using HospitexDPP.Resources;

namespace HospitexDPP.Converters
{
    public class StatusToLocalizedStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not string status || string.IsNullOrEmpty(status))
                return "";

            var parts = status.Split('_');
            var pascal = string.Concat(parts.Select(p =>
                char.ToUpper(p[0]) + p[1..]));
            var key = $"Status_{pascal}";

            return Strings.ResourceManager.GetString(key, Strings.Culture) ?? status;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
