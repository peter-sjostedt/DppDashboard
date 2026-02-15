using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using HospitexDPP.Helpers;
using HospitexDPP.Models;
using HospitexDPP.Resources;
using HospitexDPP.ViewModels;

namespace HospitexDPP.Views
{
    public partial class BrandBatchesView : UserControl
    {
        public BrandBatchesView()
        {
            InitializeComponent();
        }

        private void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource is not DependencyObject source) return;
            var row = VisualTreeHelpers.FindParent<DataGridRow>(source);
            if (row?.DataContext is BatchSummary item && DataContext is BrandBatchesViewModel vm)
                vm.EditBatchCommand.Execute(item);
        }

        private void BatchActionMenu_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.DataContext is not BatchSummary batch) return;
            if (DataContext is not BrandBatchesViewModel vm) return;

            var menu = new ContextMenu();

            var editItem = new MenuItem
            {
                Header = Strings.Action_Edit,
                Command = vm.EditBatchCommand,
                CommandParameter = batch
            };
            menu.Items.Add(editItem);

            var materialsItem = new MenuItem
            {
                Header = Strings.Drawer_ManageMaterials,
                Command = vm.ManageMaterialsCommand,
                CommandParameter = batch
            };
            menu.Items.Add(materialsItem);

            var itemsItem = new MenuItem
            {
                Header = Strings.Drawer_ViewItems,
                Command = vm.ViewItemsCommand,
                CommandParameter = batch
            };
            menu.Items.Add(itemsItem);

            menu.Items.Add(new Separator());

            var deleteItem = new MenuItem
            {
                Header = Strings.Action_Delete,
                Command = vm.DeleteBatchCommand,
                CommandParameter = batch
            };
            menu.Items.Add(deleteItem);

            btn.ContextMenu = menu;
            menu.IsOpen = true;
        }
    }
}
