using System.Globalization;
using System.Windows.Data;

namespace HospitexDPP.Converters
{
    /// <summary>
    /// Converts ISO 3166-1 alpha-2 country codes (e.g. "SE", "PT") to full country names.
    /// Uses RegionInfo when available, falls back to a built-in dictionary for common textile countries.
    /// </summary>
    public class IsoToCountryConverter : IValueConverter
    {
        private static readonly Dictionary<string, string> Fallback = new(StringComparer.OrdinalIgnoreCase)
        {
            ["SE"] = "Sweden",
            ["PT"] = "Portugal",
            ["LT"] = "Lithuania",
            ["TR"] = "Turkey",
            ["CN"] = "China",
            ["IN"] = "India",
            ["BD"] = "Bangladesh",
            ["PK"] = "Pakistan",
            ["VN"] = "Vietnam",
            ["IT"] = "Italy",
            ["DE"] = "Germany",
            ["FR"] = "France",
            ["ES"] = "Spain",
            ["PL"] = "Poland",
            ["CZ"] = "Czech Republic",
            ["RO"] = "Romania",
            ["BG"] = "Bulgaria",
            ["GB"] = "United Kingdom",
            ["US"] = "United States",
            ["NL"] = "Netherlands",
            ["BE"] = "Belgium",
            ["DK"] = "Denmark",
            ["FI"] = "Finland",
            ["NO"] = "Norway",
            ["AT"] = "Austria",
            ["CH"] = "Switzerland",
            ["GR"] = "Greece",
            ["HR"] = "Croatia",
            ["HU"] = "Hungary",
            ["SK"] = "Slovakia",
            ["SI"] = "Slovenia",
            ["EE"] = "Estonia",
            ["LV"] = "Latvia",
            ["IE"] = "Ireland",
            ["TH"] = "Thailand",
            ["ID"] = "Indonesia",
            ["MY"] = "Malaysia",
            ["PH"] = "Philippines",
            ["KR"] = "South Korea",
            ["JP"] = "Japan",
            ["TW"] = "Taiwan",
            ["MX"] = "Mexico",
            ["BR"] = "Brazil",
            ["AR"] = "Argentina",
            ["CL"] = "Chile",
            ["CO"] = "Colombia",
            ["PE"] = "Peru",
            ["MA"] = "Morocco",
            ["TN"] = "Tunisia",
            ["EG"] = "Egypt",
            ["ET"] = "Ethiopia",
            ["KE"] = "Kenya",
            ["ZA"] = "South Africa",
            ["MM"] = "Myanmar",
            ["KH"] = "Cambodia",
            ["LK"] = "Sri Lanka",
        };

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is not string code || string.IsNullOrWhiteSpace(code))
                return null;

            code = code.Trim().ToUpperInvariant();

            // Try .NET RegionInfo first
            try
            {
                var region = new RegionInfo(code);
                return region.EnglishName;
            }
            catch
            {
                // Not a recognized region code
            }

            // Fallback dictionary
            if (Fallback.TryGetValue(code, out var name))
                return name;

            // Return the code itself if we can't resolve it
            return code;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
