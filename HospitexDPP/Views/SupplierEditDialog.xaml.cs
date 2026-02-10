using System.Windows;
using HospitexDPP.ViewModels;

namespace HospitexDPP.Views
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
