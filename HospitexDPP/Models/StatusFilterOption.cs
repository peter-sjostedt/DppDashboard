using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace HospitexDPP.Models
{
    public class StatusFilterOption : INotifyPropertyChanged
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
            "active" => Application.Current.TryFindResource("Filter_Active") as string ?? "Aktiva",
            "inactive" => Application.Current.TryFindResource("Filter_Inactive") as string ?? "Inaktiva",
            _ => Application.Current.TryFindResource("Filter_All") as string ?? "Alla"
        };

        public override string ToString() => DisplayName;

        public static List<StatusFilterOption> All => new()
        {
            new() { Value = "active" },
            new() { Value = "inactive" }
        };

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
