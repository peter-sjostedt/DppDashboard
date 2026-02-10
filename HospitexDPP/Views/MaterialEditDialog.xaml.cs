using System.Windows;
using HospitexDPP.ViewModels;

namespace HospitexDPP.Views
{
    public partial class MaterialEditDialog : Window
    {
        public MaterialEditDialog(MaterialEditViewModel viewModel)
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
