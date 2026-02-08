using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows.Input;
using DppDashboard.Models;

namespace DppDashboard.ViewModels
{
    public class MaterialEditViewModel : INotifyPropertyChanged
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private int? _materialId;
        private readonly int _supplierId;
        private readonly string _tenantApiKey;

        private string _materialName = string.Empty;
        private string _materialType = string.Empty;
        private string _description = string.Empty;
        private string _statusMessage = string.Empty;
        private bool _statusIsError;
        private bool _isSaving;
        private bool _isLocked;
        private int _batchCount;

        private CompositionRow? _selectedComposition;
        private CertificationRow? _selectedCertification;
        private SupplyChainRow? _selectedSupplyChainStep;

        private readonly List<int> _deletedCompositions = new();
        private readonly List<int> _deletedCertifications = new();
        private readonly List<int> _deletedSupplyChainSteps = new();

        public event PropertyChangedEventHandler? PropertyChanged;
        public event Action<bool>? RequestClose;

        public MaterialEditViewModel(MaterialSummary? material, int supplierId, string tenantApiKey)
        {
            _materialId = material?.Id;
            _supplierId = supplierId;
            _tenantApiKey = tenantApiKey;
            IsNew = material == null;
            _dialogTitle = material == null ? "Nytt tyg" : $"Redigera tyg: {material.MaterialName}";

            if (material != null)
            {
                _materialName = material.MaterialName ?? string.Empty;
                _materialType = material.MaterialType ?? string.Empty;
                _description = material.Description ?? string.Empty;
            }

            Compositions.CollectionChanged += OnCompositionsChanged;

            SaveAllCommand = new RelayCommand(async _ => await SaveAllAsync(), _ => !IsLocked && !string.IsNullOrWhiteSpace(MaterialName) && !IsSaving && CompositionIsValid);
            CancelCommand = new RelayCommand(_ => RequestClose?.Invoke(false));

            AddCompositionCommand = new RelayCommand(_ => Compositions.Add(new CompositionRow()));
            RemoveCompositionCommand = new RelayCommand(_ => RemoveComposition(), _ => _selectedComposition != null);

            AddCertificationCommand = new RelayCommand(_ => Certifications.Add(new CertificationRow()));
            RemoveCertificationCommand = new RelayCommand(_ => RemoveCertification(), _ => _selectedCertification != null);

            AddSupplyChainStepCommand = new RelayCommand(_ => AddSupplyChainStep());
            RemoveSupplyChainStepCommand = new RelayCommand(_ => RemoveSupplyChainStep(), _ => _selectedSupplyChainStep != null);
            MoveUpSupplyChainCommand = new RelayCommand(_ => MoveSupplyChainStep(-1), _ => CanMoveSupplyChain(-1));
            MoveDownSupplyChainCommand = new RelayCommand(_ => MoveSupplyChainStep(1), _ => CanMoveSupplyChain(1));

            if (!IsNew)
                _ = LoadExistingDataAsync();
        }

        private string _dialogTitle;

        public bool IsNew { get; }
        public string DialogTitle { get => _dialogTitle; private set { _dialogTitle = value; OnPropertyChanged(); } }
        public bool IsLocked { get => _isLocked; private set { _isLocked = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsEditable)); OnPropertyChanged(nameof(LockMessage)); } }
        public bool IsEditable => !_isLocked;
        public int BatchCount { get => _batchCount; private set { _batchCount = value; OnPropertyChanged(); } }
        public string LockMessage => _isLocked ? $"Detta tyg används i {_batchCount} produktionsbatch(ar) och kan inte ändras." : string.Empty;

        public string MaterialName { get => _materialName; set { _materialName = value; OnPropertyChanged(); } }
        public string MaterialType { get => _materialType; set { _materialType = value; OnPropertyChanged(); } }
        public string Description { get => _description; set { _description = value; OnPropertyChanged(); } }
        public string StatusMessage { get => _statusMessage; set { _statusMessage = value; OnPropertyChanged(); } }
        public bool StatusIsError { get => _statusIsError; set { _statusIsError = value; OnPropertyChanged(); } }
        public bool IsSaving { get => _isSaving; set { _isSaving = value; OnPropertyChanged(); } }

        public ObservableCollection<CompositionRow> Compositions { get; } = new();
        public ObservableCollection<CertificationRow> Certifications { get; } = new();
        public ObservableCollection<SupplyChainRow> SupplyChainSteps { get; } = new();

        public CompositionRow? SelectedComposition { get => _selectedComposition; set { _selectedComposition = value; OnPropertyChanged(); } }
        public CertificationRow? SelectedCertification { get => _selectedCertification; set { _selectedCertification = value; OnPropertyChanged(); } }
        public SupplyChainRow? SelectedSupplyChainStep { get => _selectedSupplyChainStep; set { _selectedSupplyChainStep = value; OnPropertyChanged(); } }

        public decimal CompositionTotal => Compositions.Sum(c => c.ContentValue);
        public bool CompositionIsValid => Compositions.Count == 0 || CompositionTotal == 100m;
        public string CompositionStatus => Compositions.Count == 0
            ? string.Empty
            : CompositionIsValid
                ? $"Summa: {CompositionTotal:0.##}% \u2713"
                : $"Summa: {CompositionTotal:0.##}% (måste vara 100%)";

        public ICommand SaveAllCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand AddCompositionCommand { get; }
        public ICommand RemoveCompositionCommand { get; }
        public ICommand AddCertificationCommand { get; }
        public ICommand RemoveCertificationCommand { get; }
        public ICommand AddSupplyChainStepCommand { get; }
        public ICommand RemoveSupplyChainStepCommand { get; }
        public ICommand MoveUpSupplyChainCommand { get; }
        public ICommand MoveDownSupplyChainCommand { get; }

        private async Task LoadExistingDataAsync()
        {
            try
            {
                var compositionsTask = App.ApiClient.GetWithTenantKeyAsync($"/api/materials/{_materialId}/compositions", _tenantApiKey);
                var certificationsTask = App.ApiClient.GetWithTenantKeyAsync($"/api/materials/{_materialId}/certifications", _tenantApiKey);
                var supplyChainTask = App.ApiClient.GetWithTenantKeyAsync($"/api/materials/{_materialId}/supply-chain", _tenantApiKey);
                var batchesTask = App.ApiClient.GetWithTenantKeyAsync($"/api/materials/{_materialId}/batches", _tenantApiKey);

                await Task.WhenAll(compositionsTask, certificationsTask, supplyChainTask, batchesTask);

                // Check if material is used in batches
                var batches = ParseDataList<BatchUsage>(batchesTask.Result);
                if (batches != null && batches.Count > 0)
                {
                    BatchCount = batches.Count;
                    IsLocked = true;
                    DialogTitle = $"Tyg: {_materialName} (låst)";
                    StatusIsError = false;
                    StatusMessage = LockMessage;
                    CommandManager.InvalidateRequerySuggested();
                }

                var compositions = ParseDataList<MaterialComposition>(compositionsTask.Result);
                if (compositions != null)
                    foreach (var mc in compositions)
                        Compositions.Add(ToCompositionRow(mc));

                var certifications = ParseDataList<MaterialCertification>(certificationsTask.Result);
                if (certifications != null)
                    foreach (var cert in certifications)
                        Certifications.Add(ToCertificationRow(cert));

                var supplyChain = ParseDataList<MaterialSupplyChainStep>(supplyChainTask.Result);
                if (supplyChain != null)
                    foreach (var step in supplyChain.OrderBy(s => s.Sequence))
                        SupplyChainSteps.Add(ToSupplyChainRow(step));
            }
            catch (Exception ex)
            {
                StatusIsError = true;
                StatusMessage = $"Fel vid laddning: {ex.Message}";
                Debug.WriteLine($"[MaterialEdit] Load ERROR: {ex}");
            }
        }

        private async Task SaveAllAsync()
        {
            IsSaving = true;
            StatusIsError = true;
            StatusMessage = "Sparar grundinfo...";

            Debug.WriteLine($"[MaterialEdit] === SaveAllAsync START ===");
            Debug.WriteLine($"[MaterialEdit] MaterialId={_materialId}, IsNew={IsNew}, Name={MaterialName}");
            Debug.WriteLine($"[MaterialEdit] Compositions: {Compositions.Count} rows, total={CompositionTotal}%, valid={CompositionIsValid}");
            Debug.WriteLine($"[MaterialEdit] Certifications: {Certifications.Count} rows");
            Debug.WriteLine($"[MaterialEdit] SupplyChain: {SupplyChainSteps.Count} rows");
            Debug.WriteLine($"[MaterialEdit] Deleted: {_deletedCompositions.Count} comp, {_deletedCertifications.Count} cert, {_deletedSupplyChainSteps.Count} sc");
            Debug.WriteLine($"[MaterialEdit] TenantApiKey: {_tenantApiKey[..Math.Min(8, _tenantApiKey.Length)]}...");

            try
            {
                // 1. Save basic info
                var basicPayload = new Dictionary<string, object?>
                {
                    ["material_name"] = MaterialName.Trim(),
                    ["material_type"] = NullIfEmpty(MaterialType),
                    ["description"] = NullIfEmpty(Description),
                };

                string? result;
                if (IsNew)
                {
                    Debug.WriteLine($"[MaterialEdit] POST /api/suppliers/{_supplierId}/materials");
                    result = await App.ApiClient.PostWithTenantKeyAsync($"/api/suppliers/{_supplierId}/materials", basicPayload, _tenantApiKey);
                }
                else
                {
                    Debug.WriteLine($"[MaterialEdit] PUT /api/materials/{_materialId}");
                    result = await App.ApiClient.PutWithTenantKeyAsync($"/api/materials/{_materialId}", basicPayload, _tenantApiKey);
                }

                if (result == null)
                {
                    StatusMessage = "Fel: Inget svar från servern";
                    return;
                }

                Debug.WriteLine($"[MaterialEdit] BasicInfo response: {result}");

                using (var doc = JsonDocument.Parse(result))
                {
                    if (doc.RootElement.TryGetProperty("error", out var errorProp))
                    {
                        StatusMessage = $"Fel: {errorProp.GetString()}";
                        return;
                    }
                    if (!doc.RootElement.TryGetProperty("success", out var successProp) || !successProp.GetBoolean())
                    {
                        StatusMessage = "Fel: Oväntat svar från servern";
                        return;
                    }

                    if (IsNew && doc.RootElement.TryGetProperty("data", out var dataProp))
                    {
                        if (dataProp.TryGetProperty("id", out var idProp))
                            _materialId = idProp.GetInt32();
                    }
                }

                if (_materialId == null)
                {
                    StatusMessage = "Fel: Kunde inte hämta tyg-ID";
                    return;
                }

                int materialId = _materialId.Value;

                // 2. Delete removed items
                StatusMessage = "Tar bort borttagna poster...";
                foreach (var id in _deletedCompositions)
                {
                    Debug.WriteLine($"[MaterialEdit] DELETE /api/compositions/{id}");
                    var delResult = await App.ApiClient.DeleteWithTenantKeyAsync($"/api/compositions/{id}", _tenantApiKey);
                    Debug.WriteLine($"[MaterialEdit]   => {delResult}");
                }
                foreach (var id in _deletedCertifications)
                {
                    Debug.WriteLine($"[MaterialEdit] DELETE /api/material-certifications/{id}");
                    var delResult = await App.ApiClient.DeleteWithTenantKeyAsync($"/api/material-certifications/{id}", _tenantApiKey);
                    Debug.WriteLine($"[MaterialEdit]   => {delResult}");
                }
                foreach (var id in _deletedSupplyChainSteps)
                {
                    Debug.WriteLine($"[MaterialEdit] DELETE /api/supply-chain/{id}");
                    var delResult = await App.ApiClient.DeleteWithTenantKeyAsync($"/api/supply-chain/{id}", _tenantApiKey);
                    Debug.WriteLine($"[MaterialEdit]   => {delResult}");
                }

                // 3. Save compositions
                StatusMessage = "Sparar sammansättning...";
                foreach (var comp in Compositions)
                {
                    var payload = new Dictionary<string, object?>
                    {
                        ["content_name"] = NullIfEmpty(comp.ContentName),
                        ["content_value"] = comp.ContentValue,
                        ["content_source"] = NullIfEmpty(comp.ContentSource),
                        ["recycled"] = comp.Recycled ? 1 : 0,
                    };
                    if (comp.Recycled)
                    {
                        payload["recycled_percentage"] = comp.RecycledPercentage;
                        payload["recycled_input_source"] = NullIfEmpty(comp.RecycledInputSource);
                    }
                    else
                    {
                        payload["recycled_percentage"] = 0;
                    }

                    string? compResult;
                    if (comp.Id.HasValue)
                    {
                        Debug.WriteLine($"[MaterialEdit] PUT /api/compositions/{comp.Id} payload={JsonSerializer.Serialize(payload)}");
                        compResult = await App.ApiClient.PutWithTenantKeyAsync($"/api/compositions/{comp.Id}", payload, _tenantApiKey);
                    }
                    else
                    {
                        Debug.WriteLine($"[MaterialEdit] POST /api/materials/{materialId}/compositions payload={JsonSerializer.Serialize(payload)}");
                        compResult = await App.ApiClient.PostWithTenantKeyAsync($"/api/materials/{materialId}/compositions", payload, _tenantApiKey);
                    }
                    Debug.WriteLine($"[MaterialEdit]   => {compResult}");
                }

                // 4. Save certifications
                StatusMessage = "Sparar certifieringar...";
                foreach (var cert in Certifications)
                {
                    var payload = new Dictionary<string, object?>
                    {
                        ["certification"] = NullIfEmpty(cert.Certification),
                        ["certification_id"] = NullIfEmpty(cert.CertificationId),
                        ["valid_until"] = cert.ValidUntil?.ToString("yyyy-MM-dd")
                    };

                    string? certResult;
                    if (cert.Id.HasValue)
                    {
                        Debug.WriteLine($"[MaterialEdit] PUT /api/material-certifications/{cert.Id} payload={JsonSerializer.Serialize(payload)}");
                        certResult = await App.ApiClient.PutWithTenantKeyAsync($"/api/material-certifications/{cert.Id}", payload, _tenantApiKey);
                    }
                    else
                    {
                        Debug.WriteLine($"[MaterialEdit] POST /api/materials/{materialId}/certifications payload={JsonSerializer.Serialize(payload)}");
                        certResult = await App.ApiClient.PostWithTenantKeyAsync($"/api/materials/{materialId}/certifications", payload, _tenantApiKey);
                    }
                    Debug.WriteLine($"[MaterialEdit]   => {certResult}");
                }

                // 5. Save supply chain
                StatusMessage = "Sparar supply chain...";
                int seq = 1;
                foreach (var step in SupplyChainSteps)
                {
                    step.Sequence = seq++;
                    var payload = new Dictionary<string, object?>
                    {
                        ["sequence"] = step.Sequence,
                        ["process_step"] = NullIfEmpty(step.ProcessStep),
                        ["country"] = NullIfEmpty(step.Country)?.ToUpperInvariant(),
                        ["facility_name"] = NullIfEmpty(step.FacilityName),
                        ["facility_identifier"] = NullIfEmpty(step.FacilityIdentifier)
                    };

                    string? scResult;
                    if (step.Id.HasValue)
                    {
                        Debug.WriteLine($"[MaterialEdit] PUT /api/supply-chain/{step.Id} payload={JsonSerializer.Serialize(payload)}");
                        scResult = await App.ApiClient.PutWithTenantKeyAsync($"/api/supply-chain/{step.Id}", payload, _tenantApiKey);
                    }
                    else
                    {
                        Debug.WriteLine($"[MaterialEdit] POST /api/materials/{materialId}/supply-chain payload={JsonSerializer.Serialize(payload)}");
                        scResult = await App.ApiClient.PostWithTenantKeyAsync($"/api/materials/{materialId}/supply-chain", payload, _tenantApiKey);
                    }
                    Debug.WriteLine($"[MaterialEdit]   => {scResult}");
                }

                Debug.WriteLine($"[MaterialEdit] === SaveAllAsync COMPLETE ===");
                StatusIsError = false;
                RequestClose?.Invoke(true);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Fel: {ex.Message}";
                Debug.WriteLine($"[MaterialEdit] Save ERROR: {ex}");
            }
            finally
            {
                IsSaving = false;
            }
        }

        private void RemoveComposition()
        {
            if (_selectedComposition == null) return;
            if (_selectedComposition.Id.HasValue)
                _deletedCompositions.Add(_selectedComposition.Id.Value);
            Compositions.Remove(_selectedComposition);
        }

        private void RemoveCertification()
        {
            if (_selectedCertification == null) return;
            if (_selectedCertification.Id.HasValue)
                _deletedCertifications.Add(_selectedCertification.Id.Value);
            Certifications.Remove(_selectedCertification);
        }

        private void AddSupplyChainStep()
        {
            var row = new SupplyChainRow { Sequence = SupplyChainSteps.Count + 1 };
            SupplyChainSteps.Add(row);
        }

        private void RemoveSupplyChainStep()
        {
            if (_selectedSupplyChainStep == null) return;
            if (_selectedSupplyChainStep.Id.HasValue)
                _deletedSupplyChainSteps.Add(_selectedSupplyChainStep.Id.Value);
            SupplyChainSteps.Remove(_selectedSupplyChainStep);
            UpdateSequenceNumbers();
        }

        private bool CanMoveSupplyChain(int direction)
        {
            if (_selectedSupplyChainStep == null) return false;
            int index = SupplyChainSteps.IndexOf(_selectedSupplyChainStep);
            int newIndex = index + direction;
            return newIndex >= 0 && newIndex < SupplyChainSteps.Count;
        }

        private void MoveSupplyChainStep(int direction)
        {
            if (_selectedSupplyChainStep == null) return;
            int index = SupplyChainSteps.IndexOf(_selectedSupplyChainStep);
            int newIndex = index + direction;
            if (newIndex < 0 || newIndex >= SupplyChainSteps.Count) return;
            SupplyChainSteps.Move(index, newIndex);
            UpdateSequenceNumbers();
        }

        private void UpdateSequenceNumbers()
        {
            for (int i = 0; i < SupplyChainSteps.Count; i++)
                SupplyChainSteps[i].Sequence = i + 1;
        }

        private void OnCompositionsChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
                foreach (CompositionRow row in e.OldItems)
                    row.PropertyChanged -= OnCompositionRowChanged;
            if (e.NewItems != null)
                foreach (CompositionRow row in e.NewItems)
                    row.PropertyChanged += OnCompositionRowChanged;
            NotifyCompositionProperties();
        }

        private void OnCompositionRowChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(CompositionRow.ContentValue))
                NotifyCompositionProperties();
        }

        private void NotifyCompositionProperties()
        {
            OnPropertyChanged(nameof(CompositionTotal));
            OnPropertyChanged(nameof(CompositionIsValid));
            OnPropertyChanged(nameof(CompositionStatus));
            CommandManager.InvalidateRequerySuggested();
        }

        private static CompositionRow ToCompositionRow(MaterialComposition mc)
        {
            return new CompositionRow
            {
                Id = mc.Id,
                ContentName = mc.ContentName ?? string.Empty,
                ContentValue = JsonElementToDecimal(mc.ContentValue),
                ContentSource = mc.ContentSource ?? string.Empty,
                Recycled = JsonElementToBool(mc.Recycled),
                RecycledPercentage = JsonElementToDecimal(mc.RecycledPercentage),
                RecycledInputSource = mc.RecycledInputSource ?? string.Empty
            };
        }

        private static CertificationRow ToCertificationRow(MaterialCertification cert)
        {
            DateTime? validUntil = null;
            if (!string.IsNullOrEmpty(cert.ValidUntil) && DateTime.TryParse(cert.ValidUntil, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
                validUntil = dt;

            return new CertificationRow
            {
                Id = cert.Id,
                Certification = cert.Certification ?? string.Empty,
                CertificationId = cert.CertificationId ?? string.Empty,
                ValidUntil = validUntil
            };
        }

        private static SupplyChainRow ToSupplyChainRow(MaterialSupplyChainStep step)
        {
            return new SupplyChainRow
            {
                Id = step.Id,
                Sequence = step.Sequence,
                ProcessStep = step.ProcessStep ?? string.Empty,
                Country = step.Country ?? string.Empty,
                FacilityName = step.FacilityName ?? string.Empty,
                FacilityIdentifier = step.FacilityIdentifier ?? string.Empty
            };
        }

        private static decimal JsonElementToDecimal(JsonElement? element)
        {
            if (element == null || element.Value.ValueKind == JsonValueKind.Null || element.Value.ValueKind == JsonValueKind.Undefined)
                return 0;
            if (element.Value.ValueKind == JsonValueKind.Number)
                return element.Value.GetDecimal();
            if (element.Value.ValueKind == JsonValueKind.String &&
                decimal.TryParse(element.Value.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var d))
                return d;
            return 0;
        }

        private static bool JsonElementToBool(JsonElement? element)
        {
            if (element == null || element.Value.ValueKind == JsonValueKind.Null || element.Value.ValueKind == JsonValueKind.Undefined)
                return false;
            if (element.Value.ValueKind == JsonValueKind.True) return true;
            if (element.Value.ValueKind == JsonValueKind.False) return false;
            if (element.Value.ValueKind == JsonValueKind.Number)
                return element.Value.GetInt32() != 0;
            return false;
        }

        private static List<T>? ParseDataList<T>(string? json)
        {
            if (string.IsNullOrEmpty(json)) return null;
            try
            {
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("data", out var data))
                    return JsonSerializer.Deserialize<List<T>>(data.GetRawText(), JsonOptions);
            }
            catch { }
            return null;
        }

        private static string? NullIfEmpty(string? value)
        {
            var trimmed = value?.Trim();
            return string.IsNullOrEmpty(trimmed) ? null : trimmed;
        }

        private void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
