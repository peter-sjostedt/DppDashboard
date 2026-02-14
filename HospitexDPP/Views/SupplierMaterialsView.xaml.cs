using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using HospitexDPP.Helpers;
using HospitexDPP.Models;

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
                Header = Application.Current.TryFindResource("Action_Edit") as string ?? "Redigera"
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
                    ? (Application.Current.TryFindResource("Action_Deactivate") as string ?? "Inaktivera")
                    : (Application.Current.TryFindResource("Action_Activate") as string ?? "Aktivera")
            };
            toggleItem.Click += (_, _) =>
            {
                if (DataContext is ViewModels.SupplierMaterialsViewModel vm)
                    vm.ToggleActiveCommand.Execute(material);
            };
            menu.Items.Add(toggleItem);

            var deleteItem = new MenuItem
            {
                Header = Application.Current.TryFindResource("Action_Delete") as string ?? "Ta bort"
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
