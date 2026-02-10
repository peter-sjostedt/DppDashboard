using System.Windows;
using HospitexDPP.ViewModels;

namespace HospitexDPP.Views
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
