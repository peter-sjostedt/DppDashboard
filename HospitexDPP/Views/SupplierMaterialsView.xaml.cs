using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using HospitexDPP.Helpers;
using HospitexDPP.Models;
using HospitexDPP.Resources;

namespace HospitexDPP.Views
{
    public partial class SupplierMaterialsView : UserControl
    {
        public SupplierMaterialsView()
        {
            InitializeComponent();
        }

        private void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource is not DependencyObject source) return;
            var row = VisualTreeHelpers.FindParent<DataGridRow>(source);
            if (row?.DataContext is MaterialSummary item && DataContext is ViewModels.SupplierMaterialsViewModel vm)
                vm.EditCommand.Execute(item);
        }

        private void ActionMenu_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn) return;
            var material = btn.DataContext as MaterialSummary;
            if (material == null) return;

            var menu = new ContextMenu();

            var editItem = new MenuItem
            {
                Header = Strings.Action_Edit
            };
            editItem.Click += (_, _) =>
            {
                if (DataContext is ViewModels.SupplierMaterialsViewModel vm)
                    vm.EditCommand.Execute(material);
            };
            menu.Items.Add(editItem);

            var toggleItem = new MenuItem
            {
                Header = material.IsActive == 1
                    ? Strings.Action_Deactivate
                    : Strings.Action_Activate
            };
            toggleItem.Click += (_, _) =>
            {
                if (DataContext is ViewModels.SupplierMaterialsViewModel vm)
                    vm.ToggleActiveCommand.Execute(material);
            };
            menu.Items.Add(toggleItem);

            var deleteItem = new MenuItem
            {
                Header = Strings.Action_Delete
            };
            deleteItem.Click += (_, _) =>
            {
                if (DataContext is ViewModels.SupplierMaterialsViewModel vm)
                    vm.DeleteCommand.Execute(material);
            };
            menu.Items.Add(deleteItem);

            menu.PlacementTarget = btn;
            menu.IsOpen = true;
        }
    }
}
