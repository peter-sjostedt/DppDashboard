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

            NewMaterialCommand = new RelayCommand(_ => OpenNewMaterialDialog(), _ => _selectedSupplier != null && !string.IsNullOrEmpty(_currentSupplierApiKey));
            EditMaterialCommand = new RelayCommand(_ => OpenEditMaterialDialog(), _ => _selectedMaterial != null && !string.IsNullOrEmpty(_currentSupplierApiKey));
            DeleteMaterialCommand = new RelayCommand(async _ => await DeleteSelectedMaterialAsync(), _ => _selectedMaterial != null && !string.IsNullOrEmpty(_currentSupplierApiKey));

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
                    SelectedContext = "Tyger";
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
                SelectedContext = "Tyger (ingen API-nyckel)";
                return;
            }

            SelectedContext = "Tyger (...)";

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
                SelectedContext = $"Tyger ({Materials.Count})";
            }
            catch
            {
                SelectedContext = "Tyger (Fel)";
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
                            var batchesTask = _apiClient.GetWithTenantKeyAsync($"/api/materials/{materialId}/batches", apiKey);

                            await Task.WhenAll(compositionsTask, certificationsTask, supplyChainTask, batchesTask);

                            detail.Compositions ??= ParseData<List<MaterialComposition>>(compositionsTask.Result);
                            detail.Certifications ??= ParseData<List<MaterialCertification>>(certificationsTask.Result);
                            detail.SupplyChain ??= ParseData<List<MaterialSupplyChainStep>>(supplyChainTask.Result);
                            detail.Batches = ParseData<List<BatchUsage>>(batchesTask.Result) ?? new List<BatchUsage>();

                            MaterialDetail = detail;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                StatusText = $"Fel vid laddning av tyg: {ex.Message}";
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

        private void OpenNewMaterialDialog()
        {
            if (_selectedSupplier == null || string.IsNullOrEmpty(_currentSupplierApiKey)) return;
            var vm = new MaterialEditViewModel(null, _selectedSupplier.Id, _currentSupplierApiKey);
            var dialog = new MaterialEditDialog(vm)
            {
                Owner = Application.Current.MainWindow
            };
            if (dialog.ShowDialog() == true)
            {
                _ = ReloadMaterialsAsync();
            }
        }

        private void OpenEditMaterialDialog()
        {
            if (_selectedSupplier == null || _selectedMaterial == null || string.IsNullOrEmpty(_currentSupplierApiKey)) return;
            var vm = new MaterialEditViewModel(_selectedMaterial, _selectedSupplier.Id, _currentSupplierApiKey);
            var dialog = new MaterialEditDialog(vm)
            {
                Owner = Application.Current.MainWindow
            };
            if (dialog.ShowDialog() == true)
            {
                _ = ReloadMaterialsAsync();
            }
        }

        private async Task DeleteSelectedMaterialAsync()
        {
            if (_selectedMaterial == null || string.IsNullOrEmpty(_currentSupplierApiKey)) return;

            var result = MessageBox.Show(
                $"Vill du ta bort {_selectedMaterial.MaterialName}?",
                "Ta bort tyg",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            SelectedContext = "Tar bort tyg...";
            try
            {
                var json = await _apiClient.DeleteWithTenantKeyAsync($"/api/materials/{_selectedMaterial.Id}", _currentSupplierApiKey);
                Debug.WriteLine($"[Suppliers] DELETE /api/materials/{_selectedMaterial.Id} => {json}");

                if (json != null)
                {
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("success", out var s) && s.GetBoolean())
                    {
                        await ReloadMaterialsAsync();
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

        private async Task ReloadMaterialsAsync()
        {
            if (_selectedSupplier != null)
                await LoadMaterialsAsync(_selectedSupplier);
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
                $"Vill du ta bort {_selectedSupplier.SupplierName}?\n\nDetta tar även bort alla tyger kopplade till denna supplier.",
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
