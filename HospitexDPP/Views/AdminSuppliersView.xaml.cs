using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using HospitexDPP.Helpers;
using HospitexDPP.Models;
using HospitexDPP.ViewModels;

namespace HospitexDPP.Views
{
    public partial class AdminSuppliersView : UserControl
    {
        public AdminSuppliersView()
        {
            InitializeComponent();
        }

        private void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource is not DependencyObject source) return;
            var row = VisualTreeHelpers.FindParent<DataGridRow>(source);
            if (row?.DataContext is SupplierDetail item && DataContext is AdminSuppliersViewModel vm)
                vm.EditCommand.Execute(item);
        }

        private void ActionMenu_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn) return;
            var supplier = btn.DataContext as SupplierDetail;
            if (supplier == null) return;
            var vm = DataContext as AdminSuppliersViewModel;
            if (vm == null) return;

            var menu = new ContextMenu();

            menu.Items.Add(new MenuItem
            {
                Header = TryFindResource("Action_Edit") as string ?? "Redigera",
                Command = vm.EditCommand,
                CommandParameter = supplier
            });

            menu.Items.Add(new MenuItem
            {
                Header = TryFindResource("Action_ViewApiKey") as string ?? "Visa API-nyckel",
                Command = vm.ShowApiKeyCommand,
                CommandParameter = supplier
            });

            menu.Items.Add(new MenuItem
            {
                Header = TryFindResource("Action_RegenerateKey") as string ?? "Generera ny nyckel",
                Command = vm.RegenerateKeyCommand,
                CommandParameter = supplier
            });

            menu.Items.Add(new Separator());

            menu.Items.Add(new MenuItem
            {
                Header = supplier.IsActive == 1
                    ? (TryFindResource("Action_Deactivate") as string ?? "Inaktivera")
                    : (TryFindResource("Action_Activate") as string ?? "Aktivera"),
                Command = vm.ToggleActiveCommand,
                CommandParameter = supplier
            });

            menu.Items.Add(new MenuItem
            {
                Header = TryFindResource("Action_Delete") as string ?? "Ta bort",
                Command = vm.DeleteCommand,
                CommandParameter = supplier
            });

            menu.PlacementTarget = btn;
            menu.IsOpen = true;
        }
    }
}
