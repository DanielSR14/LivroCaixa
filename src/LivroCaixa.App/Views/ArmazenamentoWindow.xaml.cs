using System.Windows;
using System.Windows.Controls;
using LivroCaixa.App.ViewModels;

namespace LivroCaixa.App.Views;

public partial class ArmazenamentoWindow : Window
{
    private readonly ArmazenamentoViewModel _viewModel;

    public ArmazenamentoWindow(ArmazenamentoViewModel viewModel)
    {
        InitializeComponent();

        _viewModel = viewModel;
        DataContext = _viewModel;

        Loaded += async (_, _) => await _viewModel.InicializarAsync();
    }

    private async void BotaoLimpar_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: ItemArmazenamentoCompetencia item })
            return;

        var confirmar = MessageBox.Show(
            $"Isso vai apagar permanentemente {item.QuantidadeAnexos} comprovante(s) da competência {item.CompetenciaExibicao} desta máquina.\n\n" +
            "Os arquivos já foram enviados ao escritório na exportação — essa ação só remove a cópia local. Deseja continuar?",
            "Confirmar limpeza de comprovantes",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (confirmar == MessageBoxResult.Yes)
            await _viewModel.LimparCommand.ExecuteAsync(item);
    }
}
