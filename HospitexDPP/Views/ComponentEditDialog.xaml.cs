using System.Windows;
using HospitexDPP.Helpers;
using HospitexDPP.Models;

namespace HospitexDPP.Views
{
    public partial class ComponentEditDialog : Window
    {
        public string? SelectedComponent => (CmbComponent.SelectedItem as EnumOption)?.Value;
        public string? SelectedMaterial => (CmbMaterial.SelectedItem as EnumOption)?.Value;
        public string? ContentName => string.IsNullOrWhiteSpace(TxtContentName.Text) ? null : TxtContentName.Text.Trim();
        public string? ContentValue => string.IsNullOrWhiteSpace(TxtContentValue.Text) ? null : TxtContentValue.Text.Trim();
        public string? ContentSource => string.IsNullOrWhiteSpace(TxtContentSource.Text) ? null : TxtContentSource.Text.Trim();

        public ComponentEditDialog()
        {
            InitializeComponent();

            CmbComponent.ItemsSource = EnumMappings.GetOptions("Component");
            CmbMaterial.ItemsSource = EnumMappings.GetOptions("Material");

            if (CmbComponent.Items.Count > 0) CmbComponent.SelectedIndex = 0;
            if (CmbMaterial.Items.Count > 0) CmbMaterial.SelectedIndex = 0;
        }

        public ComponentEditDialog(ComponentInfo existing) : this()
        {
            Title = HospitexDPP.Resources.Strings.Action_Edit;
            CmbComponent.SelectedValue = existing.Component;
            CmbMaterial.SelectedValue = existing.Material;
            TxtContentName.Text = existing.ContentName ?? "";
            TxtContentValue.Text = existing.ContentValue?.ToString() ?? "";
            TxtContentSource.Text = existing.ContentSource ?? "";
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
