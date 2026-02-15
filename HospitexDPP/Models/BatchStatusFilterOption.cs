using System.ComponentModel;
using System.Runtime.CompilerServices;
using HospitexDPP.Resources;

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
            "planned" => Strings.BatchFilter_Planned,
            "in_production" => Strings.BatchFilter_InProduction,
            "completed" => Strings.BatchFilter_Completed,
            _ => Strings.Filter_All
        };

        public override string ToString() => DisplayName;

        public static List<BatchStatusFilterOption> All => new()
        {
            new() { Value = "in_production" },
            new() { Value = "completed" }
        };

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
