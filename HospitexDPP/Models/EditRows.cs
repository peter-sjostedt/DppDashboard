using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace HospitexDPP.Models
{
    public class CompositionRow : INotifyPropertyChanged
    {
        private string _contentName = string.Empty;
        private decimal _contentValue;
        private string _contentSource = string.Empty;
        private bool _recycled;
        private decimal _recycledPercentage;
        private string _recycledInputSource = string.Empty;

        public int? Id { get; set; }

        public string ContentName
        {
            get => _contentName;
            set { _contentName = value; OnPropertyChanged(); }
        }

        public decimal ContentValue
        {
            get => _contentValue;
            set { _contentValue = value; OnPropertyChanged(); }
        }

        public string ContentSource
        {
            get => _contentSource;
            set { _contentSource = value; OnPropertyChanged(); }
        }

        public bool Recycled
        {
            get => _recycled;
            set { _recycled = value; OnPropertyChanged(); }
        }

        public decimal RecycledPercentage
        {
            get => _recycledPercentage;
            set { _recycledPercentage = value; OnPropertyChanged(); }
        }

        public string RecycledInputSource
        {
            get => _recycledInputSource;
            set { _recycledInputSource = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public class CertificationRow : INotifyPropertyChanged
    {
        private string _certification = string.Empty;
        private string _certificationId = string.Empty;
        private DateTime? _validUntil;

        public int? Id { get; set; }

        public string Certification
        {
            get => _certification;
            set { _certification = value; OnPropertyChanged(); }
        }

        public string CertificationId
        {
            get => _certificationId;
            set { _certificationId = value; OnPropertyChanged(); }
        }

        public DateTime? ValidUntil
        {
            get => _validUntil;
            set { _validUntil = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public class SupplyChainRow : INotifyPropertyChanged
    {
        private int _sequence;
        private string _processStep = string.Empty;
        private string _country = string.Empty;
        private string _facilityName = string.Empty;
        private string _facilityIdentifier = string.Empty;

        public int? Id { get; set; }

        public int Sequence
        {
            get => _sequence;
            set { _sequence = value; OnPropertyChanged(); }
        }

        public string ProcessStep
        {
            get => _processStep;
            set { _processStep = value; OnPropertyChanged(); }
        }

        public string Country
        {
            get => _country;
            set { _country = value; OnPropertyChanged(); }
        }

        public string FacilityName
        {
            get => _facilityName;
            set { _facilityName = value; OnPropertyChanged(); }
        }

        public string FacilityIdentifier
        {
            get => _facilityIdentifier;
            set { _facilityIdentifier = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
