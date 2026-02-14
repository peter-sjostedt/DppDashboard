using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using HospitexDPP.Services;

namespace HospitexDPP.ViewModels
{
    public class SupplierViewModel : INotifyPropertyChanged
    {
        private int _materialCount;
        private int _brandCount;
        private int _poCount;
        private int _batchCount;

        public event PropertyChangedEventHandler? PropertyChanged;

        public SupplierViewModel()
        {
            MaterialsTab = new SupplierMaterialsViewModel();
            PurchaseOrdersTab = new SupplierPurchaseOrdersViewModel();
            BatchesTab = new SupplierBatchesViewModel();
            BrandsTab = new SupplierBrandsViewModel();

            MaterialsTab.OnDataChanged = () => MaterialCount = MaterialsTab.TotalCount;
            PurchaseOrdersTab.OnDataChanged = () => PoCount = PurchaseOrdersTab.TotalCount;
            BatchesTab.OnDataChanged = () => BatchCount = BatchesTab.TotalCount;
            BrandsTab.OnDataChanged = () => BrandCount = BrandsTab.TotalCount;

            SetLanguageCommand = new RelayCommand(lang =>
            {
                var code = lang as string ?? "sv";
                LanguageService.SetLanguage(code);
            });
        }

        public SupplierMaterialsViewModel MaterialsTab { get; }
        public SupplierPurchaseOrdersViewModel PurchaseOrdersTab { get; }
        public SupplierBatchesViewModel BatchesTab { get; }
        public SupplierBrandsViewModel BrandsTab { get; }

        public ICommand SetLanguageCommand { get; }

        public int MaterialCount
        {
            get => _materialCount;
            set { _materialCount = value; OnPropertyChanged(); OnPropertyChanged(nameof(StatusInfo)); }
        }

        public int BrandCount
        {
            get => _brandCount;
            set { _brandCount = value; OnPropertyChanged(); OnPropertyChanged(nameof(StatusInfo)); }
        }

        public int PoCount
        {
            get => _poCount;
            set { _poCount = value; OnPropertyChanged(); OnPropertyChanged(nameof(StatusInfo)); }
        }

        public int BatchCount
        {
            get => _batchCount;
            set { _batchCount = value; OnPropertyChanged(); OnPropertyChanged(nameof(StatusInfo)); }
        }

        public string StatusInfo =>
            $"{App.Session?.SupplierName ?? "Leverantör"}  |  {MaterialCount} tyger  |  {PoCount} ordrar  |  {BatchCount} batcher  |  {BrandCount} varumärken";

        private void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
