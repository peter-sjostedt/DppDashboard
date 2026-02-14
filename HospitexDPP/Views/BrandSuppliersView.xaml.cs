using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using HospitexDPP.Models;
using HospitexDPP.ViewModels;

namespace HospitexDPP.Views
{
    public partial class BrandSuppliersView : UserControl
    {
        public BrandSuppliersView()
        {
            InitializeComponent();
        }

        private void Material_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.DataContext is MaterialSummary material
                && DataContext is BrandSuppliersViewModel vm)
            {
                vm.ViewMaterialCommand.Execute(material);
            }
        }
    }
}
