using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using HospitexDPP.Helpers;
using HospitexDPP.Models;
using HospitexDPP.ViewModels;

namespace HospitexDPP.Views
{
    public partial class BrandProductsView : UserControl
    {
        public BrandProductsView()
        {
            InitializeComponent();
        }

        private void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource is not DependencyObject source) return;
            var row = VisualTreeHelpers.FindParent<DataGridRow>(source);
            if (row?.DataContext is ProductSummary item && DataContext is BrandProductsViewModel vm)
                vm.EditCommand.Execute(item);
        }

        private void ActionMenu_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.DataContext is not ProductSummary product) return;
            if (DataContext is not BrandProductsViewModel vm) return;

            var menu = new ContextMenu();

            var editItem = new MenuItem
            {
                Header = Application.Current.TryFindResource("Action_Edit") as string ?? "Redigera",
                Command = vm.EditCommand,
                CommandParameter = product
            };
            menu.Items.Add(editItem);

            var toggleText = product.IsActive == 1
                ? Application.Current.TryFindResource("Action_Deactivate") as string ?? "Inaktivera"
                : Application.Current.TryFindResource("Action_Activate") as string ?? "Aktivera";
            var toggleItem = new MenuItem
            {
                Header = toggleText,
                Command = vm.ToggleActiveCommand,
                CommandParameter = product
            };
            menu.Items.Add(toggleItem);

            menu.Items.Add(new Separator());

            var deleteItem = new MenuItem
            {
                Header = Application.Current.TryFindResource("Action_Delete") as string ?? "Ta bort",
                Command = vm.DeleteCommand,
                CommandParameter = product
            };
            menu.Items.Add(deleteItem);

            btn.ContextMenu = menu;
            menu.IsOpen = true;
        }
    }
}
