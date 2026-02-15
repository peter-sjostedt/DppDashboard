using System.ComponentModel;
using System.Runtime.CompilerServices;
using HospitexDPP.Resources;

namespace HospitexDPP.Models
{
    public class CertFilterOption : INotifyPropertyChanged
    {
        public string Value { get; set; } = "";

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set { _isSelected = value; OnPropertyChanged(); }
        }

        public string DisplayName => Value switch
        {
            "ok" => Strings.ResourceManager.GetString("CertFilter_Ok", Strings.Culture) ?? "Valid",
            "expiring" => Strings.ResourceManager.GetString("CertFilter_Expiring", Strings.Culture) ?? "Expiring",
            "expired" => Strings.ResourceManager.GetString("CertFilter_Expired", Strings.Culture) ?? "Expired",
            "none" => Strings.ResourceManager.GetString("CertFilter_None", Strings.Culture) ?? "No certs",
            _ => Strings.ResourceManager.GetString("CertFilter_All", Strings.Culture) ?? "All"
        };

        public override string ToString() => DisplayName;

        public static List<CertFilterOption> All => new()
        {
            new() { Value = "ok" },
            new() { Value = "expiring" },
            new() { Value = "expired" },
            new() { Value = "none" }
        };

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
