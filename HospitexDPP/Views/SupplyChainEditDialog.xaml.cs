using System.Windows;

namespace HospitexDPP.Views
{
    public partial class SupplyChainEditDialog : Window
    {
        public string? ProcessStep => string.IsNullOrWhiteSpace(TxtProcessStep.Text) ? null : TxtProcessStep.Text.Trim();
        public string? Country => string.IsNullOrWhiteSpace(TxtCountry.Text) ? null : TxtCountry.Text.Trim();
        public string? FacilityName => string.IsNullOrWhiteSpace(TxtFacilityName.Text) ? null : TxtFacilityName.Text.Trim();

        public SupplyChainEditDialog()
        {
            InitializeComponent();
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
