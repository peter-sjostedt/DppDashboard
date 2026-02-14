using System.Collections.ObjectModel;

namespace HospitexDPP.Models
{
    public class ProductBatchGroup
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public ObservableCollection<BatchSummary> Batches { get; set; } = new();
        public bool HasBatches => Batches.Count > 0;
    }
}
