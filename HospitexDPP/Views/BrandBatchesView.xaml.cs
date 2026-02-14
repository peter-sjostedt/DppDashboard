using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using HospitexDPP.Helpers;
using HospitexDPP.Models;
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
                Header = Application.Current.TryFindResource("Action_Edit") as string ?? "Redigera",
                Command = vm.EditBatchCommand,
                CommandParameter = batch
            };
            menu.Items.Add(editItem);

            var materialsItem = new MenuItem
            {
                Header = Application.Current.TryFindResource("Drawer_ManageMaterials") as string ?? "Hantera material",
                Command = vm.ManageMaterialsCommand,
                CommandParameter = batch
            };
            menu.Items.Add(materialsItem);

            var itemsItem = new MenuItem
            {
                Header = Application.Current.TryFindResource("Drawer_ViewItems") as string ?? "Artiklar",
                Command = vm.ViewItemsCommand,
                CommandParameter = batch
            };
            menu.Items.Add(itemsItem);

            menu.Items.Add(new Separator());

            var deleteItem = new MenuItem
            {
                Header = Application.Current.TryFindResource("Action_Delete") as string ?? "Ta bort",
                Command = vm.DeleteBatchCommand,
                CommandParameter = batch
            };
            menu.Items.Add(deleteItem);

            btn.ContextMenu = menu;
            menu.IsOpen = true;
        }
    }
}
