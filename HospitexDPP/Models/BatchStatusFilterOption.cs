using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace HospitexDPP.Models
{
    public class BatchStatusFilterOption : INotifyPropertyChanged
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
            "planned" => Application.Current.TryFindResource("BatchFilter_Planned") as string ?? "Planerad",
            "in_production" => Application.Current.TryFindResource("BatchFilter_InProduction") as string ?? "I produktion",
            "completed" => Application.Current.TryFindResource("BatchFilter_Completed") as string ?? "Klar",
            _ => Application.Current.TryFindResource("Filter_All") as string ?? "Alla"
        };

        public override string ToString() => DisplayName;

        public static List<BatchStatusFilterOption> All => new()
        {
            new() { Value = "planned" },
            new() { Value = "in_production" },
            new() { Value = "completed" }
        };

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
