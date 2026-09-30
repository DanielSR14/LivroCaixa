using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using LivroCaixa.App.ViewModels;

namespace LivroCaixa.App.Views;

public partial class LancamentoDetalheWindow : Window
{
    private readonly LancamentoDetalheViewModel _viewModel;

    public LancamentoDetalheWindow(LancamentoDetalheViewModel viewModel)
    {
        InitializeComponent();

        _viewModel = viewModel;
        DataContext = _viewModel;
    }

    private async void BotaoAbrirAnexo_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: Guid anexoId })
            return;

        try
        {
            var caminho = await _viewModel.ObterCaminhoCompletoAnexoAsync(anexoId);
            Process.Start(new ProcessStartInfo(caminho) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Não foi possível abrir o anexo: {ex.Message}", "Erro",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void BotaoFechar_Click(object sender, RoutedEventArgs e) => Close();
}
