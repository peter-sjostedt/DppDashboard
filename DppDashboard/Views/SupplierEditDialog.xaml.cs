using System.Windows;
using DppDashboard.ViewModels;

namespace DppDashboard.Views
{
    public partial class SupplierEditDialog : Window
    {
        public SupplierEditDialog(SupplierEditViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
            viewModel.RequestClose += saved =>
            {
                DialogResult = saved;
                Close();
            };
        }
    }
}
