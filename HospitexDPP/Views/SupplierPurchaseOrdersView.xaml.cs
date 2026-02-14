using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using HospitexDPP.Helpers;
using HospitexDPP.Models;

namespace HospitexDPP.Views
{
    public partial class SupplierPurchaseOrdersView : UserControl
    {
        public SupplierPurchaseOrdersView()
        {
            InitializeComponent();
        }

        private void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource is not DependencyObject source) return;
            var row = VisualTreeHelpers.FindParent<DataGridRow>(source);
            if (row?.DataContext is PurchaseOrderSummary item && DataContext is ViewModels.SupplierPurchaseOrdersViewModel vm)
                vm.SelectCommand.Execute(item);
        }

        private void ActionMenu_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn) return;
            var po = btn.DataContext as PurchaseOrderSummary;
            if (po == null) return;

            var menu = new ContextMenu();

            var viewItem = new MenuItem
            {
                Header = Application.Current.TryFindResource("Action_ViewDetails") as string ?? "Visa detaljer"
            };
            viewItem.Click += (_, _) =>
            {
                if (DataContext is ViewModels.SupplierPurchaseOrdersViewModel vm)
                    vm.SelectCommand.Execute(po);
            };
            menu.Items.Add(viewItem);

            if (po.CanAccept)
            {
                var acceptItem = new MenuItem
                {
                    Header = Application.Current.TryFindResource("Action_AcceptOrder") as string ?? "Acceptera order"
                };
                acceptItem.Click += (_, _) =>
                {
                    if (DataContext is ViewModels.SupplierPurchaseOrdersViewModel vm)
                        vm.AcceptDirectCommand.Execute(po);
                };
                menu.Items.Add(acceptItem);
            }

            menu.PlacementTarget = btn;
            menu.IsOpen = true;
        }
    }
}
