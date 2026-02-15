using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using HospitexDPP.Models;
using HospitexDPP.Resources;
using HospitexDPP.ViewModels;

namespace HospitexDPP.Views
{
    public partial class BrandSuppliersView : UserControl
    {
        public BrandSuppliersView()
        {
            InitializeComponent();
        }

        private void Material_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.DataContext is MaterialSummary material
                && DataContext is BrandSuppliersViewModel vm)
            {
                vm.ViewMaterialCommand.Execute(material);
            }
        }

        private void MaterialMenu_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.DataContext is not MaterialSummary material) return;
            if (DataContext is not BrandSuppliersViewModel vm) return;

            var menu = new ContextMenu();
            menu.Items.Add(new MenuItem
            {
                Header = Strings.Action_ViewDetails,
                Command = vm.ViewMaterialCommand,
                CommandParameter = material
            });

            btn.ContextMenu = menu;
            menu.IsOpen = true;
        }
    }
}
