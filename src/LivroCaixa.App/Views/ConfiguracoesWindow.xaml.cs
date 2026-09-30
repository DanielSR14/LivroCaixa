using System.Windows;
using LivroCaixa.App.ViewModels;
using LivroCaixa.Application;
using Microsoft.Win32;

namespace LivroCaixa.App.Views;

public partial class ConfiguracoesWindow : Window
{
    private readonly ConfiguracoesViewModel _viewModel;

    public ConfiguracoesWindow(ConfiguracoesViewModel viewModel)
    {
        InitializeComponent();

        _viewModel = viewModel;
        DataContext = _viewModel;

        Loaded += async (_, _) => await _viewModel.InicializarAsync();
    }

    private void BotaoEscolherPasta_Click(object sender, RoutedEventArgs e)
    {
        var dialogo = new OpenFolderDialog { Title = "Selecione a pasta de destino do backup" };
        if (dialogo.ShowDialog(this) == true)
            _viewModel.DefinirPastaDestino(dialogo.FolderName);
    }

    private async void BotaoNovoCodigoRecuperacao_Click(object sender, RoutedEventArgs e)
    {
        var confirmar = MessageBox.Show(
            this,
            "Gerar um novo código de recuperação? O código anterior deixa de valer assim que você confirmar que guardou o novo.",
            "Novo código de recuperação",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (confirmar != MessageBoxResult.Yes)
            return;

        var codigo = CodigoRecuperacao.Gerar();
        var confirmou = new CodigoRecuperacaoWindow(codigo, "Este é o seu novo código de recuperação. O anterior deixa de valer.")
        { Owner = this }.ShowDialog();

        if (confirmou == true)
            await _viewModel.SalvarCodigoRecuperacaoAsync(codigo);
    }

    private void BotaoTermoUso_Click(object sender, RoutedEventArgs e) =>
        new TermoUsoWindow(modoAceite: false) { Owner = this, WindowStartupLocation = WindowStartupLocation.CenterOwner }.ShowDialog();
}
