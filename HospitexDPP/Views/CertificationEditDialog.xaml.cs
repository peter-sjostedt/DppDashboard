using System.Windows;

namespace HospitexDPP.Views
{
    public partial class CertificationEditDialog : Window
    {
        public string? Certification => string.IsNullOrWhiteSpace(TxtCertification.Text) ? null : TxtCertification.Text.Trim();
        public string? CertificationId => string.IsNullOrWhiteSpace(TxtCertificationId.Text) ? null : TxtCertificationId.Text.Trim();
        public string? ValidUntil => string.IsNullOrWhiteSpace(TxtValidUntil.Text) ? null : TxtValidUntil.Text.Trim();

        public CertificationEditDialog()
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
