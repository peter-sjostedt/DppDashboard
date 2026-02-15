using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace HospitexDPP.ViewModels
{
    public class BrandViewModel : INotifyPropertyChanged
    {
        private int _productCount;
        private int _batchCount;
        private int _poCount;
        private int _selectedTabIndex;

        public event PropertyChangedEventHandler? PropertyChanged;

        public BrandViewModel()
        {
            ProductsTab = new BrandProductsViewModel();
            PurchaseOrdersTab = new BrandPurchaseOrdersViewModel();
            BatchesTab = new BrandBatchesViewModel();
            SuppliersTab = new BrandSuppliersViewModel();
            DashboardTab = new BrandDashboardViewModel(NavigateToTab, ProductsTab, SuppliersTab, PurchaseOrdersTab, BatchesTab);

            ProductsTab.OnDataChanged = () => ProductCount = ProductsTab.TotalCount;
            PurchaseOrdersTab.OnDataChanged = () => PoCount = PurchaseOrdersTab.TotalCount;
            BatchesTab.OnDataChanged = () => BatchCount = BatchesTab.TotalCount;
        }

        public BrandDashboardViewModel DashboardTab { get; }
        public BrandProductsViewModel ProductsTab { get; }
        public BrandPurchaseOrdersViewModel PurchaseOrdersTab { get; }
        public BrandBatchesViewModel BatchesTab { get; }
        public BrandSuppliersViewModel SuppliersTab { get; }

        public int SelectedTabIndex
        {
            get => _selectedTabIndex;
            set { _selectedTabIndex = value; OnPropertyChanged(); }
        }

        public void NavigateToTab(int index)
        {
            SelectedTabIndex = index;
        }

        public int ProductCount
        {
            get => _productCount;
            set { _productCount = value; OnPropertyChanged(); OnPropertyChanged(nameof(StatusInfo)); }
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
            $"{App.Session?.BrandName ?? "Brand"}  |  {ProductCount} produkter  |  {PoCount} ordrar  |  {BatchCount} batchar";

        private void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
