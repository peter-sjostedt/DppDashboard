using System.Windows;

namespace HospitexDPP.Views
{
    public partial class CompositionEditDialog : Window
    {
        public string? ContentName => string.IsNullOrWhiteSpace(TxtContentName.Text) ? null : TxtContentName.Text.Trim();
        public string? ContentValue => string.IsNullOrWhiteSpace(TxtContentValue.Text) ? null : TxtContentValue.Text.Trim();
        public string? ContentSource => string.IsNullOrWhiteSpace(TxtContentSource.Text) ? null : TxtContentSource.Text.Trim();
        public int Recycled => ChkRecycled.IsChecked == true ? 1 : 0;

        public CompositionEditDialog()
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
