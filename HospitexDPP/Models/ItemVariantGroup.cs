using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace HospitexDPP.Models
{
    public class ItemVariantGroup : INotifyPropertyChanged
    {
        public string Size { get; set; } = "";
        public string ColorBrand { get; set; } = "";
        public int Count { get; set; }
        public List<string> SerialNumbers { get; set; } = new();

        private bool _isExpanded;
        public bool IsExpanded
        {
            get => _isExpanded;
            set { _isExpanded = value; OnPropertyChanged(); }
        }

        public string DisplayName => $"{Size} · {ColorBrand} ({Count})";

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
