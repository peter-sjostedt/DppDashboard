using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using HospitexDPP.Services;

namespace HospitexDPP.ViewModels
{
    public class BrandViewModel : INotifyPropertyChanged
    {
        private int _productCount;
        private int _batchCount;

        public event PropertyChangedEventHandler? PropertyChanged;

        public BrandViewModel()
        {
            ProductsTab = new BrandProductsViewModel();
            BatchesTab = new BrandBatchesViewModel();
            SuppliersTab = new BrandSuppliersViewModel();

            ProductsTab.OnDataChanged = () => ProductCount = ProductsTab.TotalCount;
            BatchesTab.OnDataChanged = () => BatchCount = BatchesTab.TotalCount;

            SetLanguageCommand = new RelayCommand(lang =>
            {
                var code = lang as string ?? "sv";
                LanguageService.SetLanguage(code);
            });
        }

        public BrandProductsViewModel ProductsTab { get; }
        public BrandBatchesViewModel BatchesTab { get; }
        public BrandSuppliersViewModel SuppliersTab { get; }

        public ICommand SetLanguageCommand { get; }

        public int ProductCount
        {
            get => _productCount;
            set { _productCount = value; OnPropertyChanged(); OnPropertyChanged(nameof(StatusInfo)); }
        }

        public int BatchCount
        {
            get => _batchCount;
            set { _batchCount = value; OnPropertyChanged(); OnPropertyChanged(nameof(StatusInfo)); }
        }

        public string StatusInfo =>
            $"{App.Session?.BrandName ?? "Brand"}  |  {ProductCount} produkter  |  {BatchCount} batchar";

        private void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
