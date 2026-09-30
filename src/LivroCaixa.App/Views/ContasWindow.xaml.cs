using System.Windows;
using System.Windows.Controls;
using LivroCaixa.App.ViewModels;
using LivroCaixa.Application.DTOs;
using Microsoft.Extensions.DependencyInjection;

namespace LivroCaixa.App.Views;

public partial class ContasWindow : Window
{
    private readonly ContasViewModel _viewModel;
    private readonly Guid _empresaId;

    public ContasWindow(ContasViewModel viewModel, Guid empresaId)
    {
        InitializeComponent();

        _viewModel = viewModel;
        _empresaId = empresaId;
        DataContext = _viewModel;

        _viewModel.SolicitarNovaConta += async (_, _) => await AbrirNovaContaAsync();
        _viewModel.SolicitarEditarConta += async (_, conta) => await AbrirEditarContaAsync(conta);
        Loaded += async (_, _) => await _viewModel.InicializarAsync();
    }

    private async Task AbrirNovaContaAsync()
    {
        var factory = App.Services.GetRequiredService<NovaContaViewModelFactory>();
        var novaContaViewModel = factory.CriarParaNovo(_empresaId);

        var janela = new NovaContaWindow(novaContaViewModel) { Owner = this };
        if (janela.ShowDialog() == true)
            await _viewModel.RecarregarAsync();
    }

    private async Task AbrirEditarContaAsync(ContaResumoDto conta)
    {
        var factory = App.Services.GetRequiredService<NovaContaViewModelFactory>();
        var editarContaViewModel = factory.CriarParaEdicao(_empresaId, conta);

        var janela = new NovaContaWindow(editarContaViewModel) { Owner = this };
        if (janela.ShowDialog() == true)
            await _viewModel.RecarregarAsync();
    }

    private async void BotaoExcluir_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: ContaResumoDto conta })
            return;

        var confirmar = MessageBox.Show(
            $"Deseja realmente excluir a conta \"{conta.Nome}\"?",
            "Confirmar exclusão",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (confirmar == MessageBoxResult.Yes)
            await _viewModel.ExcluirContaCommand.ExecuteAsync(conta);
    }
}
