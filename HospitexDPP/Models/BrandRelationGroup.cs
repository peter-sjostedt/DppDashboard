using System.Collections.ObjectModel;

namespace HospitexDPP.Models
{
    public class BrandRelationGroup
    {
        public int BrandId { get; set; }
        public string BrandName { get; set; } = string.Empty;
        public ObservableCollection<RelationEntry> Relations { get; set; } = new();
        public string Header => $"{BrandName} ({Relations.Count})";
        public bool HasRelations => Relations.Count > 0;
    }
}
