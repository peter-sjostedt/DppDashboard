using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using HospitexDPP.Helpers;
using HospitexDPP.Models;
using HospitexDPP.Resources;

namespace HospitexDPP.Views
{
    public partial class SupplierBatchesView : UserControl
    {
        public SupplierBatchesView()
        {
            InitializeComponent();
        }

        private void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource is not DependencyObject source) return;
            var row = VisualTreeHelpers.FindParent<DataGridRow>(source);
            if (row?.DataContext is BatchSummary item && DataContext is ViewModels.SupplierBatchesViewModel vm)
                vm.EditCommand.Execute(item);
        }

        private void ActionMenu_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn) return;
            var batch = btn.DataContext as BatchSummary;
            if (batch == null) return;

            var menu = new ContextMenu();

            // View details
            var viewItem = new MenuItem
            {
                Header = Strings.Action_ViewDetails
            };
            viewItem.Click += (_, _) =>
            {
                if (DataContext is ViewModels.SupplierBatchesViewModel vm)
                    vm.EditCommand.Execute(batch);
            };
            menu.Items.Add(viewItem);

            // Mark as completed (only if in_production)
            if (batch.Status == "in_production")
            {
                var completeItem = new MenuItem
                {
                    Header = Strings.Action_MarkCompleted
                };
                completeItem.Click += (_, _) =>
                {
                    if (DataContext is ViewModels.SupplierBatchesViewModel vm)
                        vm.MarkCompletedCommand.Execute(batch);
                };
                menu.Items.Add(completeItem);

                // Split batch (only if in_production)
                var splitItem = new MenuItem
                {
                    Header = Strings.Action_SplitBatch
                };
                splitItem.Click += (_, _) =>
                {
                    if (DataContext is ViewModels.SupplierBatchesViewModel vm)
                        vm.SplitBatchCommand.Execute(batch);
                };
                menu.Items.Add(splitItem);
            }

            // Delete (only if no items)
            if ((batch.ItemCount ?? 0) == 0)
            {
                var deleteItem = new MenuItem
                {
                    Header = Strings.Action_Delete
                };
                deleteItem.Click += (_, _) =>
                {
                    if (DataContext is ViewModels.SupplierBatchesViewModel vm)
                        vm.DeleteCommand.Execute(batch);
                };
                menu.Items.Add(deleteItem);
            }

            menu.PlacementTarget = btn;
            menu.IsOpen = true;
        }
    }
}
