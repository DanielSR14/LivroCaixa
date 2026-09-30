using System.Diagnostics;
using System.Windows;
using LivroCaixa.App.ViewModels;
using Microsoft.Win32;

namespace LivroCaixa.App.Views;

public partial class ExportacaoWindow : Window
{
    private readonly ExportacaoViewModel _viewModel;

    public ExportacaoWindow(ExportacaoViewModel viewModel)
    {
        InitializeComponent();

        _viewModel = viewModel;
        DataContext = _viewModel;
    }

    private async void BotaoExportar_Click(object sender, RoutedEventArgs e)
    {
        var dialogo = new OpenFolderDialog { Title = "Selecione a pasta de destino" };
        if (dialogo.ShowDialog(this) != true)
            return;

        await _viewModel.ExportarAsync(dialogo.FolderName);
    }

    private void BotaoAbrirPasta_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel.CaminhoGerado is null)
            return;

        Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{_viewModel.CaminhoGerado}\"")
        {
            UseShellExecute = true
        });
    }

    private void BotaoCopiarDestinatario_Click(object sender, RoutedEventArgs e) =>
        Clipboard.SetText(_viewModel.EmailDestinatario);

    private void BotaoCopiarAssunto_Click(object sender, RoutedEventArgs e) =>
        Clipboard.SetText(_viewModel.EmailAssunto);

    private void BotaoCopiarCorpo_Click(object sender, RoutedEventArgs e) =>
        Clipboard.SetText(_viewModel.EmailCorpo);
}
