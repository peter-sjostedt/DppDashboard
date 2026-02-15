using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace HospitexDPP.ViewModels
{
    public class SupplierViewModel : INotifyPropertyChanged
    {
        private int _materialCount;
        private int _brandCount;
        private int _poCount;
        private int _batchCount;
        private int _selectedTabIndex;

        public event PropertyChangedEventHandler? PropertyChanged;

        public SupplierViewModel()
        {
            MaterialsTab = new SupplierMaterialsViewModel();
            PurchaseOrdersTab = new SupplierPurchaseOrdersViewModel();
            BatchesTab = new SupplierBatchesViewModel();
            BrandsTab = new SupplierBrandsViewModel();
            DashboardTab = new SupplierDashboardViewModel(NavigateToTab, MaterialsTab, PurchaseOrdersTab, BatchesTab);

            MaterialsTab.OnDataChanged = () => MaterialCount = MaterialsTab.TotalCount;
            PurchaseOrdersTab.OnDataChanged = () => PoCount = PurchaseOrdersTab.TotalCount;
            BatchesTab.OnDataChanged = () => BatchCount = BatchesTab.TotalCount;
            BrandsTab.OnDataChanged = () => BrandCount = BrandsTab.TotalCount;
        }

        public SupplierDashboardViewModel DashboardTab { get; }
        public SupplierMaterialsViewModel MaterialsTab { get; }
        public SupplierPurchaseOrdersViewModel PurchaseOrdersTab { get; }
        public SupplierBatchesViewModel BatchesTab { get; }
        public SupplierBrandsViewModel BrandsTab { get; }

        public int SelectedTabIndex
        {
            get => _selectedTabIndex;
            set { _selectedTabIndex = value; OnPropertyChanged(); }
        }

        public void NavigateToTab(int index)
        {
            SelectedTabIndex = index;
        }

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
