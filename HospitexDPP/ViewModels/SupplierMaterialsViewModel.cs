using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using HospitexDPP.Models;
using HospitexDPP.Services;

namespace HospitexDPP.ViewModels
{
    public enum MaterialDrawerMode { None, New, Edit }

    public class SupplierMaterialsViewModel : INotifyPropertyChanged
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private readonly ApiClient _apiClient;
        private List<MaterialSummary> _allMaterials = new();
        private string _searchText = string.Empty;
        private List<StatusFilterOption> _statusFilterOptions = StatusFilterOption.All;

        private MaterialDrawerMode _drawerMode = MaterialDrawerMode.None;
        private string _statusMessage = string.Empty;
        private bool _isSaving;

        // Drawer edit fields
        private int? _editMaterialId;
        private string _editMaterialName = string.Empty;
        private string _editMaterialType = string.Empty;
        private string _editDescription = string.Empty;
        private bool _editIsActive = true;

        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>Raised after materials are loaded/reloaded so parent can update counts.</summary>
        public Action? OnDataChanged;

        public SupplierMaterialsViewModel()
        {
            _apiClient = App.ApiClient;

            AddCommand = new RelayCommand(_ => OpenNewDrawer());
            EditCommand = new RelayCommand(p => OpenEditDrawer(p as MaterialSummary));
            DeleteCommand = new RelayCommand(async p => await DeleteMaterialAsync(p as MaterialSummary));
            ToggleActiveCommand = new RelayCommand(async p => await ToggleActiveAsync(p as MaterialSummary));
            SaveCommand = new RelayCommand(async _ => await SaveMaterialAsync(), _ => !string.IsNullOrWhiteSpace(EditMaterialName) && !IsSaving);
            CancelDrawerCommand = new RelayCommand(_ => CloseDrawer());
            AddCompositionCommand = new RelayCommand(async _ => await AddCompositionAsync());
            DeleteCompositionCommand = new RelayCommand(async p => await DeleteCompositionAsync(p as MaterialComposition));
            AddCertificationCommand = new RelayCommand(async _ => await AddCertificationAsync());
            DeleteCertificationCommand = new RelayCommand(async p => await DeleteCertificationAsync(p as MaterialCertification));
            AddSupplyChainCommand = new RelayCommand(async _ => await AddSupplyChainAsync());
            DeleteSupplyChainCommand = new RelayCommand(async p => await DeleteSupplyChainAsync(p as MaterialSupplyChain));
            MoveUpCommand = new RelayCommand(async p => await MoveUpAsync(p as MaterialSupplyChain));
            MoveDownCommand = new RelayCommand(async p => await MoveDownAsync(p as MaterialSupplyChain));

            var saved = SettingsService.LoadFilter("supplier_materials");
            if (saved != null)
            {
                var selected = saved.Split(',', StringSplitOptions.RemoveEmptyEntries);
                foreach (var opt in _statusFilterOptions)
                    opt.IsSelected = selected.Contains(opt.Value);
            }
            foreach (var opt in _statusFilterOptions)
                opt.PropertyChanged += OnFilterChanged;
            LanguageService.LanguageChanged += OnLanguageChanged;
            _ = LoadMaterialsAsync();
        }

        public ObservableCollection<MaterialSummary> Materials { get; } = new();
        public ObservableCollection<MaterialComposition> Compositions { get; } = new();
        public ObservableCollection<MaterialCertification> Certifications { get; } = new();
        public ObservableCollection<MaterialSupplyChain> SupplyChain { get; } = new();
        public ObservableCollection<BatchMaterialInfo> BatchUsages { get; } = new();

        public List<StatusFilterOption> StatusFilterOptions
        {
            get => _statusFilterOptions;
            private set { _statusFilterOptions = value; OnPropertyChanged(); }
        }

        private void OnFilterChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(StatusFilterOption.IsSelected))
            {
                ApplyFilter();
                var sel = _statusFilterOptions.Where(o => o.IsSelected).Select(o => o.Value);
                SettingsService.SaveFilter("supplier_materials", string.Join(",", sel));
            }
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged();
                ApplyFilter();
            }
        }

        public bool IsDrawerOpen => _drawerMode != MaterialDrawerMode.None;
        public bool IsEditMode => _drawerMode == MaterialDrawerMode.Edit;
        public bool ShowIsActive => _drawerMode == MaterialDrawerMode.Edit;
        public bool HasMaterials => Materials.Count > 0;
        public bool HasBatchUsages => BatchUsages.Count > 0;
        public int TotalCount => _allMaterials.Count;

        public string DrawerTitle
        {
            get => _drawerMode switch
            {
                MaterialDrawerMode.New => Application.Current.TryFindResource("Drawer_NewMaterial") as string ?? "Nytt tyg",
                MaterialDrawerMode.Edit => Application.Current.TryFindResource("Drawer_EditMaterial") as string ?? "Redigera tyg",
                _ => string.Empty
            };
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        public bool IsSaving
        {
            get => _isSaving;
            set { _isSaving = value; OnPropertyChanged(); }
        }

        // Edit properties
        public string EditMaterialName
        {
            get => _editMaterialName;
            set { _editMaterialName = value; OnPropertyChanged(); }
        }

        public string EditMaterialType
        {
            get => _editMaterialType;
            set { _editMaterialType = value; OnPropertyChanged(); }
        }

        public string EditDescription
        {
            get => _editDescription;
            set { _editDescription = value; OnPropertyChanged(); }
        }

        public bool EditIsActive
        {
            get => _editIsActive;
            set { _editIsActive = value; OnPropertyChanged(); }
        }

        // Commands
        public ICommand AddCommand { get; }
        public ICommand EditCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand ToggleActiveCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand CancelDrawerCommand { get; }
        public ICommand AddCompositionCommand { get; }
        public ICommand DeleteCompositionCommand { get; }
        public ICommand AddCertificationCommand { get; }
        public ICommand DeleteCertificationCommand { get; }
        public ICommand AddSupplyChainCommand { get; }
        public ICommand DeleteSupplyChainCommand { get; }
        public ICommand MoveUpCommand { get; }
        public ICommand MoveDownCommand { get; }

        private void SetDrawerMode(MaterialDrawerMode mode)
        {
            _drawerMode = mode;
            OnPropertyChanged(nameof(IsDrawerOpen));
            OnPropertyChanged(nameof(IsEditMode));
            OnPropertyChanged(nameof(ShowIsActive));
            OnPropertyChanged(nameof(DrawerTitle));
        }

        private void OpenNewDrawer()
        {
            _editMaterialId = null;
            EditMaterialName = string.Empty;
            EditMaterialType = string.Empty;
            EditDescription = string.Empty;
            EditIsActive = true;
            Compositions.Clear();
            Certifications.Clear();
            SupplyChain.Clear();
            BatchUsages.Clear();
            StatusMessage = string.Empty;
            OnPropertyChanged(nameof(HasBatchUsages));
            SetDrawerMode(MaterialDrawerMode.New);
        }

        private async void OpenEditDrawer(MaterialSummary? material)
        {
            if (material == null) return;
            _editMaterialId = material.Id;
            StatusMessage = string.Empty;

            EditMaterialName = material.MaterialName ?? string.Empty;
            EditMaterialType = material.MaterialType ?? string.Empty;
            EditDescription = material.Description ?? string.Empty;
            EditIsActive = material.IsActive == 1;

            try
            {
                // Load compositions
                Compositions.Clear();
                var compJson = await _apiClient.GetWithTenantKeyAsync($"/api/materials/{material.Id}/compositions", App.Session!.SupplierKey!);
                if (compJson != null)
                {
                    var compDoc = JsonDocument.Parse(compJson);
                    if (compDoc.RootElement.TryGetProperty("data", out var compArr))
                    {
                        var items = JsonSerializer.Deserialize<List<MaterialComposition>>(compArr.GetRawText(), JsonOptions);
                        if (items != null) foreach (var c in items) Compositions.Add(c);
                    }
                }

                // Load certifications
                Certifications.Clear();
                var certJson = await _apiClient.GetWithTenantKeyAsync($"/api/materials/{material.Id}/certifications", App.Session!.SupplierKey!);
                if (certJson != null)
                {
                    var certDoc = JsonDocument.Parse(certJson);
                    if (certDoc.RootElement.TryGetProperty("data", out var certArr))
                    {
                        var items = JsonSerializer.Deserialize<List<MaterialCertification>>(certArr.GetRawText(), JsonOptions);
                        if (items != null) foreach (var c in items) Certifications.Add(c);
                    }
                }

                // Load supply chain
                SupplyChain.Clear();
                var scJson = await _apiClient.GetWithTenantKeyAsync($"/api/materials/{material.Id}/supply-chain", App.Session!.SupplierKey!);
                if (scJson != null)
                {
                    var scDoc = JsonDocument.Parse(scJson);
                    if (scDoc.RootElement.TryGetProperty("data", out var scArr))
                    {
                        var items = JsonSerializer.Deserialize<List<MaterialSupplyChain>>(scArr.GetRawText(), JsonOptions);
                        if (items != null) foreach (var s in items) SupplyChain.Add(s);
                    }
                }

                // Load batch usages
                BatchUsages.Clear();
                var batchJson = await _apiClient.GetWithTenantKeyAsync($"/api/materials/{material.Id}/batches", App.Session!.SupplierKey!);
                if (batchJson != null)
                {
                    var batchDoc = JsonDocument.Parse(batchJson);
                    if (batchDoc.RootElement.TryGetProperty("data", out var batchArr))
                    {
                        var items = JsonSerializer.Deserialize<List<BatchMaterialInfo>>(batchArr.GetRawText(), JsonOptions);
                        if (items != null) foreach (var b in items) BatchUsages.Add(b);
                    }
                }
                OnPropertyChanged(nameof(HasBatchUsages));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SupplierMaterials] OpenEditDrawer error: {ex.Message}");
            }

            SetDrawerMode(MaterialDrawerMode.Edit);
        }

        private void CloseDrawer()
        {
            SetDrawerMode(MaterialDrawerMode.None);
        }

        private async Task SaveMaterialAsync()
        {
            IsSaving = true;
            StatusMessage = Application.Current.TryFindResource("Msg_Saving") as string ?? "Sparar...";

            try
            {
                var payload = new Dictionary<string, object?>
                {
                    ["material_name"] = EditMaterialName.Trim(),
                    ["material_type"] = NullIfEmpty(EditMaterialType),
                    ["description"] = NullIfEmpty(EditDescription),
                    ["_is_active"] = EditIsActive ? 1 : 0
                };

                string? result;
                if (_drawerMode == MaterialDrawerMode.New)
                {
                    var supplierId = App.Session!.SupplierId!.Value;
                    result = await _apiClient.PostWithTenantKeyAsync($"/api/suppliers/{supplierId}/materials", payload, App.Session!.SupplierKey!);
                }
                else
                {
                    result = await _apiClient.PutWithTenantKeyAsync($"/api/materials/{_editMaterialId}", payload, App.Session!.SupplierKey!);
                }

                if (result != null)
                {
                    using var doc = JsonDocument.Parse(result);
                    if (doc.RootElement.TryGetProperty("success", out var s) && s.GetBoolean())
                    {
                        CloseDrawer();
                        await ReloadMaterialsAsync();
                        return;
                    }
                    if (doc.RootElement.TryGetProperty("error", out var err))
                    {
                        StatusMessage = $"Fel: {err.GetString()}";
                        return;
                    }
                }
                StatusMessage = Application.Current.TryFindResource("Msg_NoResponse") as string ?? "Inget svar från servern";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Fel: {ex.Message}";
            }
            finally
            {
                IsSaving = false;
            }
        }

        private async Task DeleteMaterialAsync(MaterialSummary? material)
        {
            if (material == null) return;

            try
            {
                // Check batch usage first
                var batchJson = await _apiClient.GetWithTenantKeyAsync($"/api/materials/{material.Id}/batches", App.Session!.SupplierKey!);
                var hasBatchUsage = false;
                if (batchJson != null)
                {
                    var batchDoc = JsonDocument.Parse(batchJson);
                    if (batchDoc.RootElement.TryGetProperty("data", out var batchArr))
                    {
                        var items = JsonSerializer.Deserialize<List<BatchMaterialInfo>>(batchArr.GetRawText(), JsonOptions);
                        hasBatchUsage = items != null && items.Count > 0;
                    }
                }

                if (hasBatchUsage)
                {
                    // Material is used in batches — offer deactivation instead
                    var deactivateText = Application.Current.TryFindResource("Confirm_DeactivateMaterial") as string
                        ?? "Tyget används i batcher och kan inte tas bort. Vill du inaktivera det istället?";
                    var deactivateResult = MessageBox.Show(
                        $"{deactivateText}\n\n{material.MaterialName}",
                        Application.Current.TryFindResource("Action_Deactivate") as string ?? "Inaktivera",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning);

                    if (deactivateResult != MessageBoxResult.Yes) return;

                    var payload = new Dictionary<string, object?>
                    {
                        ["material_name"] = material.MaterialName,
                        ["_is_active"] = 0
                    };
                    var putJson = await _apiClient.PutWithTenantKeyAsync($"/api/materials/{material.Id}", payload, App.Session!.SupplierKey!);
                    if (putJson != null)
                    {
                        using var doc = JsonDocument.Parse(putJson);
                        if (doc.RootElement.TryGetProperty("success", out var s) && s.GetBoolean())
                        {
                            await ReloadMaterialsAsync();
                            return;
                        }
                    }
                }
                else
                {
                    // No batch usage — safe to delete
                    var confirmText = Application.Current.TryFindResource("Confirm_Delete") as string ?? "Är du säker?";
                    var result = MessageBox.Show(
                        $"{confirmText}\n\n{material.MaterialName}",
                        Application.Current.TryFindResource("Action_Delete") as string ?? "Ta bort",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning);

                    if (result != MessageBoxResult.Yes) return;

                    var json = await _apiClient.DeleteWithTenantKeyAsync($"/api/materials/{material.Id}", App.Session!.SupplierKey!);
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
                            MessageBox.Show($"Fel: {err.GetString()}", "Fel", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fel: {ex.Message}", "Fel", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task ToggleActiveAsync(MaterialSummary? material)
        {
            if (material == null) return;

            var newActive = material.IsActive == 1 ? 0 : 1;
            var payload = new Dictionary<string, object?>
            {
                ["material_name"] = material.MaterialName,
                ["_is_active"] = newActive
            };

            try
            {
                var json = await _apiClient.PutWithTenantKeyAsync($"/api/materials/{material.Id}", payload, App.Session!.SupplierKey!);
                if (json != null)
                {
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("success", out var s) && s.GetBoolean())
                    {
                        await ReloadMaterialsAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SupplierMaterials] ToggleActive error: {ex.Message}");
            }
        }

        private async Task AddCompositionAsync()
        {
            try
            {
                var payload = new Dictionary<string, object?>();
                var json = await _apiClient.PostWithTenantKeyAsync($"/api/materials/{_editMaterialId}/compositions", payload, App.Session!.SupplierKey!);
                if (json != null)
                {
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("success", out var s) && s.GetBoolean())
                    {
                        await ReloadCompositionsAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SupplierMaterials] AddComposition error: {ex.Message}");
            }
        }

        private async Task DeleteCompositionAsync(MaterialComposition? comp)
        {
            if (comp == null) return;

            var confirmText = Application.Current.TryFindResource("Confirm_Delete") as string ?? "Är du säker?";
            var result = MessageBox.Show(
                confirmText,
                Application.Current.TryFindResource("Action_Delete") as string ?? "Ta bort",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                var json = await _apiClient.DeleteWithTenantKeyAsync($"/api/compositions/{comp.Id}", App.Session!.SupplierKey!);
                if (json != null)
                {
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("success", out var s) && s.GetBoolean())
                    {
                        await ReloadCompositionsAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SupplierMaterials] DeleteComposition error: {ex.Message}");
            }
        }

        private async Task AddCertificationAsync()
        {
            try
            {
                var payload = new Dictionary<string, object?>();
                var json = await _apiClient.PostWithTenantKeyAsync($"/api/materials/{_editMaterialId}/certifications", payload, App.Session!.SupplierKey!);
                if (json != null)
                {
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("success", out var s) && s.GetBoolean())
                    {
                        await ReloadCertificationsAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SupplierMaterials] AddCertification error: {ex.Message}");
            }
        }

        private async Task DeleteCertificationAsync(MaterialCertification? cert)
        {
            if (cert == null) return;

            var confirmText = Application.Current.TryFindResource("Confirm_Delete") as string ?? "Är du säker?";
            var result = MessageBox.Show(
                confirmText,
                Application.Current.TryFindResource("Action_Delete") as string ?? "Ta bort",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                var json = await _apiClient.DeleteWithTenantKeyAsync($"/api/material-certifications/{cert.Id}", App.Session!.SupplierKey!);
                if (json != null)
                {
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("success", out var s) && s.GetBoolean())
                    {
                        await ReloadCertificationsAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SupplierMaterials] DeleteCertification error: {ex.Message}");
            }
        }

        private async Task AddSupplyChainAsync()
        {
            try
            {
                var payload = new Dictionary<string, object?>();
                var json = await _apiClient.PostWithTenantKeyAsync($"/api/materials/{_editMaterialId}/supply-chain", payload, App.Session!.SupplierKey!);
                if (json != null)
                {
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("success", out var s) && s.GetBoolean())
                    {
                        await ReloadSupplyChainAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SupplierMaterials] AddSupplyChain error: {ex.Message}");
            }
        }

        private async Task DeleteSupplyChainAsync(MaterialSupplyChain? step)
        {
            if (step == null) return;

            var confirmText = Application.Current.TryFindResource("Confirm_Delete") as string ?? "Är du säker?";
            var result = MessageBox.Show(
                confirmText,
                Application.Current.TryFindResource("Action_Delete") as string ?? "Ta bort",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                var json = await _apiClient.DeleteWithTenantKeyAsync($"/api/supply-chain/{step.Id}", App.Session!.SupplierKey!);
                if (json != null)
                {
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("success", out var s) && s.GetBoolean())
                    {
                        await ReloadSupplyChainAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SupplierMaterials] DeleteSupplyChain error: {ex.Message}");
            }
        }

        private async Task MoveUpAsync(MaterialSupplyChain? step)
        {
            if (step == null) return;

            var index = -1;
            for (int i = 0; i < SupplyChain.Count; i++)
            {
                if (SupplyChain[i].Id == step.Id) { index = i; break; }
            }

            if (index <= 0) return;

            var above = SupplyChain[index - 1];
            var currentSeq = step.Sequence?.ValueKind == JsonValueKind.Number ? step.Sequence.Value.GetInt32() : index + 1;
            var aboveSeq = above.Sequence?.ValueKind == JsonValueKind.Number ? above.Sequence.Value.GetInt32() : index;

            try
            {
                var payload1 = new Dictionary<string, object?> { ["sequence"] = aboveSeq };
                var payload2 = new Dictionary<string, object?> { ["sequence"] = currentSeq };

                await _apiClient.PutWithTenantKeyAsync($"/api/supply-chain/{step.Id}", payload1, App.Session!.SupplierKey!);
                await _apiClient.PutWithTenantKeyAsync($"/api/supply-chain/{above.Id}", payload2, App.Session!.SupplierKey!);

                await ReloadSupplyChainAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SupplierMaterials] MoveUp error: {ex.Message}");
            }
        }

        private async Task MoveDownAsync(MaterialSupplyChain? step)
        {
            if (step == null) return;

            var index = -1;
            for (int i = 0; i < SupplyChain.Count; i++)
            {
                if (SupplyChain[i].Id == step.Id) { index = i; break; }
            }

            if (index < 0 || index >= SupplyChain.Count - 1) return;

            var below = SupplyChain[index + 1];
            var currentSeq = step.Sequence?.ValueKind == JsonValueKind.Number ? step.Sequence.Value.GetInt32() : index + 1;
            var belowSeq = below.Sequence?.ValueKind == JsonValueKind.Number ? below.Sequence.Value.GetInt32() : index + 2;

            try
            {
                var payload1 = new Dictionary<string, object?> { ["sequence"] = belowSeq };
                var payload2 = new Dictionary<string, object?> { ["sequence"] = currentSeq };

                await _apiClient.PutWithTenantKeyAsync($"/api/supply-chain/{step.Id}", payload1, App.Session!.SupplierKey!);
                await _apiClient.PutWithTenantKeyAsync($"/api/supply-chain/{below.Id}", payload2, App.Session!.SupplierKey!);

                await ReloadSupplyChainAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SupplierMaterials] MoveDown error: {ex.Message}");
            }
        }

        private async Task LoadMaterialsAsync()
        {
            try
            {
                var json = await _apiClient.GetWithTenantKeyAsync("/api/materials", App.Session!.SupplierKey!);
                if (json != null)
                {
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("data", out var dataArray))
                    {
                        var items = JsonSerializer.Deserialize<List<MaterialSummary>>(dataArray.GetRawText(), JsonOptions);
                        if (items != null)
                            _allMaterials = items;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SupplierMaterials] Load error: {ex.Message}");
            }

            ApplyFilter();
            OnDataChanged?.Invoke();
        }

        private async Task ReloadMaterialsAsync()
        {
            _allMaterials.Clear();
            Materials.Clear();
            await LoadMaterialsAsync();
        }

        private async Task ReloadCompositionsAsync()
        {
            Compositions.Clear();
            var json = await _apiClient.GetWithTenantKeyAsync($"/api/materials/{_editMaterialId}/compositions", App.Session!.SupplierKey!);
            if (json != null)
            {
                var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("data", out var arr))
                {
                    var items = JsonSerializer.Deserialize<List<MaterialComposition>>(arr.GetRawText(), JsonOptions);
                    if (items != null) foreach (var c in items) Compositions.Add(c);
                }
            }
        }

        private async Task ReloadCertificationsAsync()
        {
            Certifications.Clear();
            var json = await _apiClient.GetWithTenantKeyAsync($"/api/materials/{_editMaterialId}/certifications", App.Session!.SupplierKey!);
            if (json != null)
            {
                var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("data", out var arr))
                {
                    var items = JsonSerializer.Deserialize<List<MaterialCertification>>(arr.GetRawText(), JsonOptions);
                    if (items != null) foreach (var c in items) Certifications.Add(c);
                }
            }
        }

        private async Task ReloadSupplyChainAsync()
        {
            SupplyChain.Clear();
            var json = await _apiClient.GetWithTenantKeyAsync($"/api/materials/{_editMaterialId}/supply-chain", App.Session!.SupplierKey!);
            if (json != null)
            {
                var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("data", out var arr))
                {
                    var items = JsonSerializer.Deserialize<List<MaterialSupplyChain>>(arr.GetRawText(), JsonOptions);
                    if (items != null) foreach (var s in items) SupplyChain.Add(s);
                }
            }
        }

        private void ApplyFilter()
        {
            Materials.Clear();
            var filter = _searchText.Trim();
            IEnumerable<MaterialSummary> filtered = _allMaterials;

            if (!string.IsNullOrEmpty(filter))
                filtered = filtered.Where(m => m.MaterialName != null &&
                    m.MaterialName.Contains(filter, StringComparison.OrdinalIgnoreCase));

            var activeFilters = _statusFilterOptions.Where(o => o.IsSelected).Select(o => o.Value).ToHashSet();
            if (activeFilters.Count > 0)
                filtered = filtered.Where(m =>
                    (activeFilters.Contains("active") && m.IsActive == 1) ||
                    (activeFilters.Contains("inactive") && m.IsActive != 1));

            foreach (var m in filtered)
                Materials.Add(m);

            OnPropertyChanged(nameof(HasMaterials));
        }

        private void OnLanguageChanged()
        {
            var selectedValues = _statusFilterOptions.Where(o => o.IsSelected).Select(o => o.Value).ToHashSet();
            foreach (var opt in _statusFilterOptions)
                opt.PropertyChanged -= OnFilterChanged;
            _statusFilterOptions = StatusFilterOption.All;
            foreach (var opt in _statusFilterOptions)
            {
                opt.IsSelected = selectedValues.Contains(opt.Value);
                opt.PropertyChanged += OnFilterChanged;
            }
            OnPropertyChanged(nameof(StatusFilterOptions));
            ApplyFilter();
        }

        private static string? NullIfEmpty(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
