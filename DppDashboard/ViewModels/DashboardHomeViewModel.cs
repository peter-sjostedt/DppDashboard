using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json;
using DppDashboard.Services;

namespace DppDashboard.ViewModels
{
    public class DashboardHomeViewModel : INotifyPropertyChanged
    {
        private readonly ApiClient _apiClient;
        private int _brandCount;
        private int _supplierCount;
        private int _productCount;
        private int _variantCount;
        private int _batchCount;
        private int _itemCount;
        private string _statusText = "Laddar statistik...";

        public event PropertyChangedEventHandler? PropertyChanged;

        public DashboardHomeViewModel()
        {
            _apiClient = App.ApiClient;
            _ = LoadStatsAsync();
        }

        public int BrandCount { get => _brandCount; set { _brandCount = value; OnPropertyChanged(); } }
        public int SupplierCount { get => _supplierCount; set { _supplierCount = value; OnPropertyChanged(); } }
        public int ProductCount { get => _productCount; set { _productCount = value; OnPropertyChanged(); } }
        public int VariantCount { get => _variantCount; set { _variantCount = value; OnPropertyChanged(); } }
        public int BatchCount { get => _batchCount; set { _batchCount = value; OnPropertyChanged(); } }
        public int ItemCount { get => _itemCount; set { _itemCount = value; OnPropertyChanged(); } }
        public string StatusText { get => _statusText; set { _statusText = value; OnPropertyChanged(); } }

        private async Task LoadStatsAsync()
        {
            try
            {
                var json = await _apiClient.GetRawAsync("/api/admin/stats");
                if (json != null)
                {
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("data", out var data))
                    {
                        BrandCount = GetInt(data, "brands");
                        SupplierCount = GetInt(data, "suppliers");
                        ProductCount = GetInt(data, "products");
                        VariantCount = GetInt(data, "variants");
                        BatchCount = GetInt(data, "batches");
                        ItemCount = GetInt(data, "items");
                    }
                }
                StatusText = "Ansluten till dpp.petersjostedt.se";
            }
            catch (Exception ex)
            {
                StatusText = $"Fel: {ex.Message}";
            }
        }

        private static int GetInt(JsonElement element, string property)
        {
            if (element.TryGetProperty(property, out var val) && val.ValueKind == JsonValueKind.Number)
                return val.GetInt32();
            return 0;
        }

        private void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
