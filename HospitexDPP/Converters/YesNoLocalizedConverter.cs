using System.Globalization;
using System.Windows.Data;
using HospitexDPP.Resources;

namespace HospitexDPP.Converters
{
    public class YesNoLocalizedConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not string s || string.IsNullOrEmpty(s))
                return value ?? "";

            return s.ToLowerInvariant() switch
            {
                "yes" => Strings.ResourceManager.GetString("Value_Yes", Strings.Culture) ?? s,
                "no" => Strings.ResourceManager.GetString("Value_No", Strings.Culture) ?? s,
                _ => s
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not string s || string.IsNullOrEmpty(s))
                return value ?? "";

            var yes = Strings.ResourceManager.GetString("Value_Yes", Strings.Culture) ?? "Yes";
            var no = Strings.ResourceManager.GetString("Value_No", Strings.Culture) ?? "No";

            if (s.Equals(yes, StringComparison.OrdinalIgnoreCase))
                return "Yes";
            if (s.Equals(no, StringComparison.OrdinalIgnoreCase))
                return "No";

            return s;
        }
    }
}
