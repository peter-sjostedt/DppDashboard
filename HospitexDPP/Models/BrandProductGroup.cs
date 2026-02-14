using System.Collections.ObjectModel;
using System.Windows;

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
                var template = Application.Current.TryFindResource("StatusBar_Products") as string ?? "{0} produkter";
                return $"({string.Format(template, ProductCount)})";
            }
        }
    }
}
