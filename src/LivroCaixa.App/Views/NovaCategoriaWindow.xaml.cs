using System.Windows;
using LivroCaixa.App.ViewModels;

namespace LivroCaixa.App.Views;

public partial class NovaCategoriaWindow : Window
{
    public NovaCategoriaWindow(NovaCategoriaViewModel viewModel)
    {
        InitializeComponent();

        DataContext = viewModel;
        viewModel.Concluido += (_, _) =>
        {
            DialogResult = true;
            Close();
        };
    }
}
