using System.Windows;

namespace HospitexDPP.Services
{
    public static class LanguageService
    {
        private static readonly Uri SvUri = new("Resources/Strings.sv.xaml", UriKind.Relative);
        private static readonly Uri EnUri = new("Resources/Strings.en.xaml", UriKind.Relative);

        private static ResourceDictionary? _currentStrings;

        public static string CurrentLanguage { get; private set; } = "sv";

        public static event Action? LanguageChanged;

        public static void Initialize()
        {
            var settings = SettingsService.Load();
            SetLanguage(settings.Language, save: false);
        }

        public static void SetLanguage(string lang, bool save = true)
        {
            var uri = lang == "en" ? EnUri : SvUri;
            var newDict = new ResourceDictionary { Source = uri };

            var merged = Application.Current.Resources.MergedDictionaries;

            if (_currentStrings != null)
                merged.Remove(_currentStrings);

            merged.Add(newDict);
            _currentStrings = newDict;
            CurrentLanguage = lang;

            if (save)
            {
                var settings = SettingsService.Load();
                settings.Language = lang;
                SettingsService.Save(settings);
            }

            LanguageChanged?.Invoke();
        }
    }
}
