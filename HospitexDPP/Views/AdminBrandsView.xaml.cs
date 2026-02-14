using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using HospitexDPP.Helpers;
using HospitexDPP.Models;
using HospitexDPP.ViewModels;

namespace HospitexDPP.Views
{
    public partial class AdminBrandsView : UserControl
    {
        public AdminBrandsView()
        {
            InitializeComponent();
        }

        private void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource is not DependencyObject source) return;
            var row = VisualTreeHelpers.FindParent<DataGridRow>(source);
            if (row?.DataContext is BrandSummary item && DataContext is AdminBrandsViewModel vm)
                vm.EditCommand.Execute(item);
        }

        private void ActionMenu_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn) return;
            var brand = btn.DataContext as BrandSummary;
            if (brand == null) return;
            var vm = DataContext as AdminBrandsViewModel;
            if (vm == null) return;

            var menu = new ContextMenu();

            menu.Items.Add(new MenuItem
            {
                Header = TryFindResource("Action_Edit") as string ?? "Redigera",
                Command = vm.EditCommand,
                CommandParameter = brand
            });

            menu.Items.Add(new MenuItem
            {
                Header = TryFindResource("Action_ViewApiKey") as string ?? "Visa API-nyckel",
                Command = vm.ShowApiKeyCommand,
                CommandParameter = brand
            });

            menu.Items.Add(new MenuItem
            {
                Header = TryFindResource("Action_RegenerateKey") as string ?? "Generera ny nyckel",
                Command = vm.RegenerateKeyCommand,
                CommandParameter = brand
            });

            menu.Items.Add(new Separator());

            menu.Items.Add(new MenuItem
            {
                Header = brand.IsActive == 1
                    ? (TryFindResource("Action_Deactivate") as string ?? "Inaktivera")
                    : (TryFindResource("Action_Activate") as string ?? "Aktivera"),
                Command = vm.ToggleActiveCommand,
                CommandParameter = brand
            });

            menu.Items.Add(new MenuItem
            {
                Header = TryFindResource("Action_Delete") as string ?? "Ta bort",
                Command = vm.DeleteCommand,
                CommandParameter = brand
            });

            menu.PlacementTarget = btn;
            menu.IsOpen = true;
        }
    }
}
