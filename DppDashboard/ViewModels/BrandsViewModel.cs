using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using DppDashboard.Models;
using DppDashboard.Services;
using DppDashboard.Views;

namespace DppDashboard.ViewModels
{
    public class BrandsViewModel : INotifyPropertyChanged
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private readonly ApiClient _apiClient;
        private BrandSummary? _selectedBrand;
        private ProductSummary? _selectedProduct;
        private ProductDetail? _productDetail;
        private string _selectedContext = string.Empty;
        private string _statusText = "Brands";
        private bool _isLoadingProduct;
        private string? _currentBrandApiKey;

        public event PropertyChangedEventHandler? PropertyChanged;

        public BrandsViewModel()
        {
            _apiClient = App.ApiClient;

            NewBrandCommand = new RelayCommand(_ => OpenNewBrandDialog());
            EditBrandCommand = new RelayCommand(_ => OpenEditBrandDialog(), _ => _selectedBrand != null);
            DeleteBrandCommand = new RelayCommand(async _ => await DeleteSelectedBrandAsync(), _ => _selectedBrand != null);

            NewProductCommand = new RelayCommand(_ => OpenNewProductDialog(), _ => _selectedBrand != null && !string.IsNullOrEmpty(_currentBrandApiKey));
            EditProductCommand = new RelayCommand(_ => OpenEditProductDialog(), _ => _selectedProduct != null && _productDetail != null && !string.IsNullOrEmpty(_currentBrandApiKey));
            DeleteProductCommand = new RelayCommand(async _ => await DeleteSelectedProductAsync(), _ => _selectedProduct != null && !string.IsNullOrEmpty(_currentBrandApiKey));

            _ = LoadBrandsAsync();
        }

        public ObservableCollection<BrandSummary> Brands { get; } = new();
        public ObservableCollection<ProductSummary> Products { get; } = new();

        public ICommand NewBrandCommand { get; }
        public ICommand EditBrandCommand { get; }
        public ICommand DeleteBrandCommand { get; }

        public ICommand NewProductCommand { get; }
        public ICommand EditProductCommand { get; }
        public ICommand DeleteProductCommand { get; }

        public BrandSummary? SelectedBrand
        {
            get => _selectedBrand;
            set
            {
                _selectedBrand = value;
                OnPropertyChanged();
                ProductDetail = null;
                _selectedProduct = null;
                OnPropertyChanged(nameof(SelectedProduct));

                if (value != null)
                {
                    SelectedContext = "Produkter";
                    _currentBrandApiKey = value.ApiKey;
                    _ = LoadProductsAsync(value);
                }
            }
        }

        public ProductSummary? SelectedProduct
        {
            get => _selectedProduct;
            set
            {
                _selectedProduct = value;
                OnPropertyChanged();
                if (value != null && _currentBrandApiKey != null)
                    _ = LoadProductDetailAsync(value.Id, _currentBrandApiKey);
            }
        }

        public ProductDetail? ProductDetail
        {
            get => _productDetail;
            set { _productDetail = value; OnPropertyChanged(); }
        }

        public bool IsLoadingProduct
        {
            get => _isLoadingProduct;
            set { _isLoadingProduct = value; OnPropertyChanged(); }
        }

        public string SelectedContext
        {
            get => _selectedContext;
            set { _selectedContext = value; OnPropertyChanged(); }
        }

        public string StatusText
        {
            get => _statusText;
            set { _statusText = value; OnPropertyChanged(); }
        }

        private async Task LoadBrandsAsync()
        {
            try
            {
                var json = await _apiClient.GetRawAsync("/api/admin/brands");
                if (json != null)
                {
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("data", out var dataArray))
                    {
                        var items = JsonSerializer.Deserialize<List<BrandSummary>>(dataArray.GetRawText(), JsonOptions);
                        if (items != null)
                            foreach (var item in items)
                                Brands.Add(item);
                    }
                }
                StatusText = $"Brands ({Brands.Count})";
            }
            catch (Exception ex)
            {
                StatusText = $"Fel: {ex.Message}";
            }
        }

        private async Task LoadProductsAsync(BrandSummary brand)
        {
            Products.Clear();
            ProductDetail = null;

            if (string.IsNullOrEmpty(brand.ApiKey))
            {
                SelectedContext = "Produkter (ingen API-nyckel)";
                return;
            }

            SelectedContext = "Produkter (...)";

            try
            {
                var json = await _apiClient.GetWithTenantKeyAsync("/api/products", brand.ApiKey);
                if (json != null)
                {
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("data", out var dataArray))
                    {
                        var items = JsonSerializer.Deserialize<List<ProductSummary>>(dataArray.GetRawText(), JsonOptions);
                        if (items != null)
                            foreach (var item in items)
                                Products.Add(item);
                    }
                }
                SelectedContext = $"Produkter ({Products.Count})";
            }
            catch
            {
                SelectedContext = "Produkter (Fel)";
            }
        }

        private async Task LoadProductDetailAsync(int productId, string apiKey)
        {
            IsLoadingProduct = true;
            ProductDetail = null;

            try
            {
                var json = await _apiClient.GetWithTenantKeyAsync($"/api/products/{productId}", apiKey);
                if (json != null)
                {
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("data", out var data))
                    {
                        var detail = JsonSerializer.Deserialize<ProductDetail>(data.GetRawText(), JsonOptions);
                        if (detail != null)
                        {
                            var careTask = _apiClient.GetWithTenantKeyAsync($"/api/products/{productId}/care", apiKey);
                            var complianceTask = _apiClient.GetWithTenantKeyAsync($"/api/products/{productId}/compliance", apiKey);
                            var circularityTask = _apiClient.GetWithTenantKeyAsync($"/api/products/{productId}/circularity", apiKey);
                            var sustainabilityTask = _apiClient.GetWithTenantKeyAsync($"/api/products/{productId}/sustainability", apiKey);
                            var variantsTask = _apiClient.GetWithTenantKeyAsync($"/api/products/{productId}/variants", apiKey);

                            await Task.WhenAll(careTask, complianceTask, circularityTask, sustainabilityTask, variantsTask);

                            detail.Care ??= ParseData<CareInfo>(careTask.Result);
                            detail.Compliance ??= ParseData<ComplianceInfo>(complianceTask.Result);
                            detail.Circularity ??= ParseData<CircularityInfo>(circularityTask.Result);
                            detail.Sustainability ??= ParseData<SustainabilityInfo>(sustainabilityTask.Result);
                            detail.Variants ??= ParseData<List<VariantInfo>>(variantsTask.Result);

                            ProductDetail = detail;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                StatusText = $"Fel vid laddning av produkt: {ex.Message}";
            }
            finally
            {
                IsLoadingProduct = false;
            }
        }

        private void OpenNewBrandDialog()
        {
            var vm = new BrandEditViewModel(null);
            var dialog = new BrandEditDialog(vm)
            {
                Owner = Application.Current.MainWindow
            };
            if (dialog.ShowDialog() == true)
            {
                _ = ReloadBrandsAsync();
            }
        }

        private void OpenEditBrandDialog()
        {
            if (_selectedBrand == null) return;
            var vm = new BrandEditViewModel(_selectedBrand);
            var dialog = new BrandEditDialog(vm)
            {
                Owner = Application.Current.MainWindow
            };
            if (dialog.ShowDialog() == true)
            {
                _ = ReloadBrandsAsync();
            }
        }

        private async Task DeleteSelectedBrandAsync()
        {
            if (_selectedBrand == null) return;

            var result = MessageBox.Show(
                $"Vill du ta bort {_selectedBrand.BrandName}?\n\nDetta tar även bort alla produkter, batchar och items.",
                "Ta bort brand",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            StatusText = "Tar bort brand...";
            try
            {
                var json = await _apiClient.DeleteAsync($"/api/admin/brands/{_selectedBrand.Id}");
                Debug.WriteLine($"[Brands] DELETE /api/admin/brands/{_selectedBrand.Id} => {json}");

                if (json != null)
                {
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("success", out var s) && s.GetBoolean())
                    {
                        await ReloadBrandsAsync();
                        return;
                    }
                    if (doc.RootElement.TryGetProperty("error", out var err))
                    {
                        StatusText = $"Fel: {err.GetString()}";
                        return;
                    }
                }
                StatusText = "Fel: Inget svar från servern";
            }
            catch (Exception ex)
            {
                StatusText = $"Fel: {ex.Message}";
            }
        }

        private async Task ReloadBrandsAsync()
        {
            Brands.Clear();
            Products.Clear();
            ProductDetail = null;
            SelectedBrand = null;
            await LoadBrandsAsync();
        }

        private void OpenNewProductDialog()
        {
            if (_selectedBrand == null || string.IsNullOrEmpty(_currentBrandApiKey)) return;
            var vm = new ProductEditViewModel(null, _selectedBrand.Id, _currentBrandApiKey);
            var dialog = new ProductEditDialog(vm)
            {
                Owner = Application.Current.MainWindow
            };
            if (dialog.ShowDialog() == true)
            {
                _ = ReloadProductsAsync();
            }
        }

        private void OpenEditProductDialog()
        {
            if (_selectedBrand == null || _productDetail == null || string.IsNullOrEmpty(_currentBrandApiKey)) return;
            var vm = new ProductEditViewModel(_productDetail, _selectedBrand.Id, _currentBrandApiKey);
            var dialog = new ProductEditDialog(vm)
            {
                Owner = Application.Current.MainWindow
            };
            if (dialog.ShowDialog() == true)
            {
                _ = ReloadProductsAsync();
            }
        }

        private async Task DeleteSelectedProductAsync()
        {
            if (_selectedProduct == null || string.IsNullOrEmpty(_currentBrandApiKey)) return;

            var result = MessageBox.Show(
                $"Vill du ta bort {_selectedProduct.ProductName}?\n\nDetta tar även bort varianter, batchar och items.",
                "Ta bort produkt",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            SelectedContext = "Tar bort produkt...";
            try
            {
                var json = await _apiClient.DeleteWithTenantKeyAsync($"/api/products/{_selectedProduct.Id}", _currentBrandApiKey);
                Debug.WriteLine($"[Brands] DELETE /api/products/{_selectedProduct.Id} => {json}");

                if (json != null)
                {
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("success", out var s) && s.GetBoolean())
                    {
                        await ReloadProductsAsync();
                        return;
                    }
                    if (doc.RootElement.TryGetProperty("error", out var err))
                    {
                        SelectedContext = $"Fel: {err.GetString()}";
                        return;
                    }
                }
                SelectedContext = "Fel: Inget svar från servern";
            }
            catch (Exception ex)
            {
                SelectedContext = $"Fel: {ex.Message}";
            }
        }

        private async Task ReloadProductsAsync()
        {
            if (_selectedBrand != null)
                await LoadProductsAsync(_selectedBrand);
        }

        private T? ParseData<T>(string? json)
        {
            if (string.IsNullOrEmpty(json)) return default;
            try
            {
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("data", out var data))
                    return JsonSerializer.Deserialize<T>(data.GetRawText(), JsonOptions);
            }
            catch { }
            return default;
        }

        private void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
