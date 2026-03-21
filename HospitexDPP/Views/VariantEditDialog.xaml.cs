using System.Windows;
using HospitexDPP.Models;
using HospitexDPP.Resources;

namespace HospitexDPP.Views
{
    public partial class VariantEditDialog : Window
    {
        public string? ItemNumber => string.IsNullOrWhiteSpace(TxtItemNumber.Text) ? null : TxtItemNumber.Text.Trim();
        public string? Size => string.IsNullOrWhiteSpace(TxtSize.Text) ? null : TxtSize.Text.Trim();
        public string? ColorBrand => string.IsNullOrWhiteSpace(TxtColorBrand.Text) ? null : TxtColorBrand.Text.Trim();
        public string? ColorGeneral => string.IsNullOrWhiteSpace(TxtColorGeneral.Text) ? null : TxtColorGeneral.Text.Trim();
        public string? Gtin => string.IsNullOrWhiteSpace(TxtGtin.Text) ? null : TxtGtin.Text.Trim();

        public VariantEditDialog()
        {
            InitializeComponent();
        }

        public VariantEditDialog(VariantInfo existing) : this()
        {
            Title = Strings.Action_Edit;
            TxtItemNumber.Text = existing.ItemNumber ?? "";
            TxtSize.Text = existing.Size ?? "";
            TxtColorBrand.Text = existing.ColorBrand ?? "";
            TxtColorGeneral.Text = existing.ColorGeneral ?? "";
            TxtGtin.Text = existing.Gtin ?? "";
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
