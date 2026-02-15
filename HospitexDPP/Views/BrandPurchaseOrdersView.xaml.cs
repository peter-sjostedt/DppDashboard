using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using HospitexDPP.Helpers;
using HospitexDPP.Models;
using HospitexDPP.Resources;
using HospitexDPP.ViewModels;

namespace HospitexDPP.Views
{
    public partial class BrandPurchaseOrdersView : UserControl
    {
        public BrandPurchaseOrdersView()
        {
            InitializeComponent();
        }

        private void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource is not DependencyObject source) return;
            var row = VisualTreeHelpers.FindParent<DataGridRow>(source);
            if (row?.DataContext is PurchaseOrderSummary item && DataContext is BrandPurchaseOrdersViewModel vm)
                vm.EditCommand.Execute(item);
        }

        private void ActionMenu_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.DataContext is not PurchaseOrderSummary order) return;
            if (DataContext is not BrandPurchaseOrdersViewModel vm) return;

            var menu = new ContextMenu();

            // Edit / View
            var editItem = new MenuItem
            {
                Header = Strings.Action_Edit,
                Command = vm.EditCommand,
                CommandParameter = order
            };
            menu.Items.Add(editItem);

            // Send (only if draft)
            if (order.Status == "draft")
            {
                var sendItem = new MenuItem
                {
                    Header = Strings.Action_Send,
                    Command = vm.SendCommand,
                    CommandParameter = order
                };
                menu.Items.Add(sendItem);
            }

            // Mark fulfilled (only if accepted)
            if (order.Status == "accepted")
            {
                var fulfillItem = new MenuItem
                {
                    Header = Strings.Action_MarkFulfilled,
                    Command = vm.FulfillCommand,
                    CommandParameter = order
                };
                menu.Items.Add(fulfillItem);
            }

            // Cancel (draft or sent)
            if (order.Status is "draft" or "sent")
            {
                var cancelItem = new MenuItem
                {
                    Header = Strings.Action_CancelOrder,
                    Command = vm.CancelOrderCommand,
                    CommandParameter = order
                };
                menu.Items.Add(cancelItem);
            }

            // Delete (only if draft)
            if (order.Status == "draft")
            {
                menu.Items.Add(new Separator());
                var deleteItem = new MenuItem
                {
                    Header = Strings.Action_Delete,
                    Command = vm.DeleteCommand,
                    CommandParameter = order
                };
                menu.Items.Add(deleteItem);
            }

            btn.ContextMenu = menu;
            menu.IsOpen = true;
        }
    }
}
