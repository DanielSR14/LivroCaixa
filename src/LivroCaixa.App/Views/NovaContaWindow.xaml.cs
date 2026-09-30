using System.Windows;
using LivroCaixa.App.ViewModels;

namespace LivroCaixa.App.Views;

public partial class NovaContaWindow : Window
{
    public NovaContaWindow(NovaContaViewModel viewModel)
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
