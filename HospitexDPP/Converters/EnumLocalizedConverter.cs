using System.Globalization;
using System.Windows.Data;
using HospitexDPP.Helpers;

namespace HospitexDPP.Converters
{
    public class EnumLocalizedConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not string dbValue || parameter is not string prefix)
                return value ?? "";
            return EnumMappings.Localize(prefix, dbValue);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
