using System.Windows;
using DppDashboard.ViewModels;

namespace DppDashboard.Views
{
    public partial class BrandEditDialog : Window
    {
        public BrandEditDialog(BrandEditViewModel viewModel)
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
