using System.Collections.ObjectModel;

namespace HospitexDPP.Models
{
    public class SupplierMaterialGroup
    {
        public int SupplierId { get; set; }
        public string SupplierName { get; set; } = string.Empty;
        public string? SupplierLocation { get; set; }
        public ObservableCollection<MaterialSummary> Materials { get; set; } = new();
        public int MaterialCount => Materials.Count;
    }
}
