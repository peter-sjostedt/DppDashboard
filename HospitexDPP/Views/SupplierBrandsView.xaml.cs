using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using HospitexDPP.Helpers;
using HospitexDPP.Models;
using HospitexDPP.Resources;

namespace HospitexDPP.Views
{
    public partial class SupplierBrandsView : UserControl
    {
        public SupplierBrandsView()
        {
            InitializeComponent();
        }

        private void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource is not DependencyObject source) return;
            var row = VisualTreeHelpers.FindParent<DataGridRow>(source);
            if (row?.DataContext is ProductSummary item && DataContext is ViewModels.SupplierBrandsViewModel vm)
                vm.ViewProductCommand.Execute(item);
        }

        private void ActionMenu_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn) return;
            var product = btn.DataContext as ProductSummary;
            if (product == null) return;

            var menu = new ContextMenu();

            var viewItem = new MenuItem
            {
                Header = Strings.Action_ViewDetails
            };
            viewItem.Click += (_, _) =>
            {
                if (DataContext is ViewModels.SupplierBrandsViewModel vm)
                    vm.ViewProductCommand.Execute(product);
            };
            menu.Items.Add(viewItem);

            menu.PlacementTarget = btn;
            menu.IsOpen = true;
        }
    }
}
