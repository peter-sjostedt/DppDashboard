using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows.Input;
using HospitexDPP.Services;

namespace HospitexDPP.ViewModels
{
    public class AdminViewModel : INotifyPropertyChanged
    {
        private readonly ApiClient _apiClient;
        private int _brandCount;
        private int _supplierCount;
        private string _statusInfo = string.Empty;

        public event PropertyChangedEventHandler? PropertyChanged;

        public AdminViewModel()
        {
            _apiClient = App.ApiClient;

            BrandsTab = new AdminBrandsViewModel();
            SuppliersTab = new AdminSuppliersViewModel();
            RelationsTab = new AdminRelationsViewModel();

            BrandsTab.OnDataChanged = () => BrandCount = BrandsTab.TotalCount;
            SuppliersTab.OnDataChanged = () => SupplierCount = SuppliersTab.TotalCount;

            SetLanguageCommand = new RelayCommand(lang =>
            {
                var code = lang as string ?? "sv";
                LanguageService.SetLanguage(code);
            });

            _ = LoadStatsAsync();
        }

        public AdminBrandsViewModel BrandsTab { get; }
        public AdminSuppliersViewModel SuppliersTab { get; }
        public AdminRelationsViewModel RelationsTab { get; }

        public ICommand SetLanguageCommand { get; }

        public int BrandCount
        {
            get => _brandCount;
            set { _brandCount = value; OnPropertyChanged(); OnPropertyChanged(nameof(StatusInfo)); }
        }

        public int SupplierCount
        {
            get => _supplierCount;
            set { _supplierCount = value; OnPropertyChanged(); OnPropertyChanged(nameof(StatusInfo)); }
        }

        public string StatusInfo =>
            $"Admin  |  {BrandCount} varumärken  |  {SupplierCount} leverantörer";

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
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Admin] Stats error: {ex.Message}");
            }
        }

        private static int GetInt(JsonElement el, string prop)
        {
            if (el.TryGetProperty(prop, out var val) && val.TryGetInt32(out var n))
                return n;
            return 0;
        }

        private void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
