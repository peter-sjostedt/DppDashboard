using System.Windows;
using DppDashboard.ViewModels;

namespace DppDashboard.Views
{
    public partial class ProductEditDialog : Window
    {
        public ProductEditDialog(ProductEditViewModel viewModel)
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
