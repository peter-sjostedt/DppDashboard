using System.Globalization;
using HospitexDPP.Helpers;
using HospitexDPP.Resources;

namespace HospitexDPP.Services
{
    public static class LanguageService
    {
        public static string CurrentLanguage { get; private set; } = "sv";

        public static event Action? LanguageChanged;

        public static void Initialize()
        {
            var settings = SettingsService.Load();
            SetLanguage(settings.Language, save: false);
        }

        public static void SetLanguage(string lang, bool save = true)
        {
            var culture = new CultureInfo(lang);
            Thread.CurrentThread.CurrentUICulture = culture;
            Strings.Culture = culture;
            CurrentLanguage = lang;

            if (save)
            {
                var settings = SettingsService.Load();
                settings.Language = lang;
                SettingsService.Save(settings);
            }

            TranslationSource.Instance.Refresh();
            LanguageChanged?.Invoke();
        }
    }
}
