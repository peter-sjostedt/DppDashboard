using System.ComponentModel;
using System.Windows.Data;
using HospitexDPP.Resources;

namespace HospitexDPP.Helpers
{
    public class TranslationSource : INotifyPropertyChanged
    {
        public static TranslationSource Instance { get; } = new();

        public string this[string key] =>
            Strings.ResourceManager.GetString(key, Strings.Culture) ?? key;

        public event PropertyChangedEventHandler? PropertyChanged;

        public void Refresh()
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(Binding.IndexerName));
        }
    }
}
