using System.Collections.ObjectModel;
using HospitexDPP.Resources;

namespace HospitexDPP.Models
{
    public class BrandProductGroup
    {
        public int BrandId { get; set; }
        public string BrandName { get; set; } = string.Empty;
        public string? BrandAddress { get; set; }
        public ObservableCollection<ProductSummary> Products { get; set; } = new();
        public int ProductCount => Products.Count;

        public string ProductCountLabel
        {
            get
            {
                return $"({string.Format(Strings.StatusBar_Products, ProductCount)})";
            }
        }
    }
}
