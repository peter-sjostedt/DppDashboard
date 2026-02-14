using System.Windows;
using System.Windows.Controls;
using HospitexDPP.Models;
using HospitexDPP.ViewModels;

namespace HospitexDPP.Views
{
    public partial class AdminRelationsView : UserControl
    {
        public AdminRelationsView()
        {
            InitializeComponent();
        }

        private void BrandMenu_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn) return;
            var group = btn.DataContext as BrandRelationGroup;
            if (group == null) return;
            var vm = DataContext as AdminRelationsViewModel;
            if (vm == null) return;

            var menu = new ContextMenu();

            menu.Items.Add(new MenuItem
            {
                Header = TryFindResource("Action_AddSupplier") as string ?? "Lägg till leverantör",
                Command = vm.AddSupplierForBrandCommand,
                CommandParameter = group
            });

            menu.PlacementTarget = btn;
            menu.IsOpen = true;
        }

        private void ActionMenu_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn) return;
            var relation = btn.DataContext as RelationEntry;
            if (relation == null) return;
            var vm = DataContext as AdminRelationsViewModel;
            if (vm == null) return;

            var menu = new ContextMenu();

            menu.Items.Add(new MenuItem
            {
                Header = relation.IsActive == 1
                    ? (TryFindResource("Action_Deactivate") as string ?? "Inaktivera")
                    : (TryFindResource("Action_Activate") as string ?? "Aktivera"),
                Command = vm.ToggleActiveCommand,
                CommandParameter = relation
            });

            menu.Items.Add(new MenuItem
            {
                Header = TryFindResource("Action_Delete") as string ?? "Ta bort",
                Command = vm.DeleteCommand,
                CommandParameter = relation
            });

            menu.PlacementTarget = btn;
            menu.IsOpen = true;
        }
    }
}
