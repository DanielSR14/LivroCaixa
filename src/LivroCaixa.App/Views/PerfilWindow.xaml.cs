using System.Windows;
using LivroCaixa.App.ViewModels;

namespace LivroCaixa.App.Views;

public partial class PerfilWindow : Window
{
    private readonly PerfilViewModel _viewModel;

    public PerfilWindow(PerfilViewModel viewModel)
    {
        InitializeComponent();

        _viewModel = viewModel;
        DataContext = _viewModel;

        Loaded += async (_, _) => await _viewModel.InicializarAsync();
    }

    private void BotaoFechar_Click(object sender, RoutedEventArgs e) => Close();
}
