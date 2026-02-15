using System.ComponentModel;
using System.Runtime.CompilerServices;
using HospitexDPP.Resources;

namespace HospitexDPP.Models
{
    public class CompletenessFilterOption : INotifyPropertyChanged
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
            "complete" => Strings.ResourceManager.GetString("CompletenessFilter_Complete", Strings.Culture) ?? "Complete",
            "incomplete" => Strings.ResourceManager.GetString("CompletenessFilter_Incomplete", Strings.Culture) ?? "Incomplete",
            _ => Value
        };

        public override string ToString() => DisplayName;

        public static List<CompletenessFilterOption> All => new()
        {
            new() { Value = "complete" },
            new() { Value = "incomplete" }
        };

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
