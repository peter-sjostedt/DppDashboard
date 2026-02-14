using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using HospitexDPP.Helpers;
using HospitexDPP.Models;

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
                Header = Application.Current.TryFindResource("Action_ViewDetails") as string ?? "Visa detaljer"
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
