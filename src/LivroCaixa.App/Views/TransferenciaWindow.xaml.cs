using System.Windows;
using LivroCaixa.App.ViewModels;

namespace LivroCaixa.App.Views;

public partial class TransferenciaWindow : Window
{
    public TransferenciaWindow(TransferenciaViewModel viewModel)
    {
        InitializeComponent();

        DataContext = viewModel;
        viewModel.Concluido += (_, _) => DialogResult = true;
        Loaded += async (_, _) => await viewModel.InicializarAsync();
    }
}
