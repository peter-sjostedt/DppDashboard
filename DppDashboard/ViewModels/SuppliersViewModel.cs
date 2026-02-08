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
    public class SuppliersViewModel : INotifyPropertyChanged
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private readonly ApiClient _apiClient;
        private SupplierDetail? _selectedSupplier;
        private MaterialSummary? _selectedMaterial;
        private MaterialDetail? _materialDetail;
        private string _selectedContext = string.Empty;
        private string _statusText = "Suppliers";
        private bool _isLoadingMaterial;
        private string? _currentSupplierApiKey;

        public event PropertyChangedEventHandler? PropertyChanged;

        public SuppliersViewModel()
        {
            _apiClient = App.ApiClient;

            NewSupplierCommand = new RelayCommand(_ => OpenNewSupplierDialog());
            EditSupplierCommand = new RelayCommand(_ => OpenEditSupplierDialog(), _ => _selectedSupplier != null);
            DeleteSupplierCommand = new RelayCommand(async _ => await DeleteSelectedSupplierAsync(), _ => _selectedSupplier != null);

            NewMaterialCommand = new RelayCommand(_ => MessageBox.Show("Nytt material - kommer snart", "Nytt material"), _ => _selectedSupplier != null);
            EditMaterialCommand = new RelayCommand(_ => MessageBox.Show($"Redigera material: {_selectedMaterial?.MaterialName}", "Redigera material"), _ => _selectedMaterial != null);
            DeleteMaterialCommand = new RelayCommand(_ => MessageBox.Show($"Ta bort {_selectedMaterial?.MaterialName}?", "Ta bort material", MessageBoxButton.YesNo), _ => _selectedMaterial != null);

            _ = LoadSuppliersAsync();
        }

        public ObservableCollection<SupplierDetail> Suppliers { get; } = new();
        public ObservableCollection<MaterialSummary> Materials { get; } = new();

        public ICommand NewSupplierCommand { get; }
        public ICommand EditSupplierCommand { get; }
        public ICommand DeleteSupplierCommand { get; }

        public ICommand NewMaterialCommand { get; }
        public ICommand EditMaterialCommand { get; }
        public ICommand DeleteMaterialCommand { get; }

        public SupplierDetail? SelectedSupplier
        {
            get => _selectedSupplier;
            set
            {
                _selectedSupplier = value;
                OnPropertyChanged();
                MaterialDetail = null;
                _selectedMaterial = null;
                OnPropertyChanged(nameof(SelectedMaterial));

                if (value != null)
                {
                    SelectedContext = "Material";
                    _currentSupplierApiKey = value.ApiKey;
                    _ = LoadMaterialsAsync(value);
                }
            }
        }

        public MaterialSummary? SelectedMaterial
        {
            get => _selectedMaterial;
            set
            {
                _selectedMaterial = value;
                OnPropertyChanged();
                if (value != null && _currentSupplierApiKey != null)
                    _ = LoadMaterialDetailAsync(value.Id, _currentSupplierApiKey);
            }
        }

        public MaterialDetail? MaterialDetail
        {
            get => _materialDetail;
            set { _materialDetail = value; OnPropertyChanged(); }
        }

        public bool IsLoadingMaterial
        {
            get => _isLoadingMaterial;
            set { _isLoadingMaterial = value; OnPropertyChanged(); }
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

        private async Task LoadSuppliersAsync()
        {
            try
            {
                var json = await _apiClient.GetRawAsync("/api/admin/suppliers");
                if (json != null)
                {
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("data", out var dataArray))
                    {
                        var items = JsonSerializer.Deserialize<List<SupplierDetail>>(dataArray.GetRawText(), JsonOptions);
                        if (items != null)
                            foreach (var item in items)
                                Suppliers.Add(item);
                    }
                }
                StatusText = $"Suppliers ({Suppliers.Count})";
            }
            catch (Exception ex)
            {
                StatusText = $"Fel: {ex.Message}";
            }
        }

        private async Task LoadMaterialsAsync(SupplierDetail supplier)
        {
            Materials.Clear();
            MaterialDetail = null;

            if (string.IsNullOrEmpty(supplier.ApiKey))
            {
                SelectedContext = "Material (ingen API-nyckel)";
                return;
            }

            SelectedContext = "Material (...)";

            try
            {
                var json = await _apiClient.GetWithTenantKeyAsync("/api/materials", supplier.ApiKey);
                if (json != null)
                {
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("data", out var dataArray))
                    {
                        var items = JsonSerializer.Deserialize<List<MaterialSummary>>(dataArray.GetRawText(), JsonOptions);
                        if (items != null)
                            foreach (var item in items)
                                Materials.Add(item);
                    }
                }
                SelectedContext = $"Material ({Materials.Count})";
            }
            catch
            {
                SelectedContext = "Material (Fel)";
            }
        }

        private async Task LoadMaterialDetailAsync(int materialId, string apiKey)
        {
            IsLoadingMaterial = true;
            MaterialDetail = null;

            try
            {
                var json = await _apiClient.GetWithTenantKeyAsync($"/api/materials/{materialId}", apiKey);
                if (json != null)
                {
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("data", out var data))
                    {
                        var detail = JsonSerializer.Deserialize<MaterialDetail>(data.GetRawText(), JsonOptions);
                        if (detail != null)
                        {
                            var compositionsTask = _apiClient.GetWithTenantKeyAsync($"/api/materials/{materialId}/compositions", apiKey);
                            var certificationsTask = _apiClient.GetWithTenantKeyAsync($"/api/materials/{materialId}/certifications", apiKey);
                            var supplyChainTask = _apiClient.GetWithTenantKeyAsync($"/api/materials/{materialId}/supply-chain", apiKey);

                            await Task.WhenAll(compositionsTask, certificationsTask, supplyChainTask);

                            detail.Compositions ??= ParseData<List<MaterialComposition>>(compositionsTask.Result);
                            detail.Certifications ??= ParseData<List<MaterialCertification>>(certificationsTask.Result);
                            detail.SupplyChain ??= ParseData<List<MaterialSupplyChainStep>>(supplyChainTask.Result);

                            MaterialDetail = detail;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                StatusText = $"Fel vid laddning av material: {ex.Message}";
            }
            finally
            {
                IsLoadingMaterial = false;
            }
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

        private void OpenNewSupplierDialog()
        {
            var vm = new SupplierEditViewModel(null);
            var dialog = new SupplierEditDialog(vm)
            {
                Owner = Application.Current.MainWindow
            };
            if (dialog.ShowDialog() == true)
            {
                _ = ReloadSuppliersAsync();
            }
        }

        private void OpenEditSupplierDialog()
        {
            if (_selectedSupplier == null) return;
            var vm = new SupplierEditViewModel(_selectedSupplier);
            var dialog = new SupplierEditDialog(vm)
            {
                Owner = Application.Current.MainWindow
            };
            if (dialog.ShowDialog() == true)
            {
                _ = ReloadSuppliersAsync();
            }
        }

        private async Task DeleteSelectedSupplierAsync()
        {
            if (_selectedSupplier == null) return;

            var result = MessageBox.Show(
                $"Vill du ta bort {_selectedSupplier.SupplierName}?\n\nDetta tar även bort alla material kopplade till denna supplier.",
                "Ta bort supplier",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            StatusText = "Tar bort supplier...";
            try
            {
                var json = await _apiClient.DeleteAsync($"/api/admin/suppliers/{_selectedSupplier.Id}");
                Debug.WriteLine($"[Suppliers] DELETE /api/admin/suppliers/{_selectedSupplier.Id} => {json}");

                if (json != null)
                {
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("success", out var s) && s.GetBoolean())
                    {
                        await ReloadSuppliersAsync();
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

        private async Task ReloadSuppliersAsync()
        {
            Suppliers.Clear();
            Materials.Clear();
            MaterialDetail = null;
            SelectedSupplier = null;
            await LoadSuppliersAsync();
        }

        private void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
