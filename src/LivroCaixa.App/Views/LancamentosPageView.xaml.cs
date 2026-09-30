using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using LivroCaixa.App.ViewModels;
using LivroCaixa.Application.DTOs;
using Microsoft.Extensions.DependencyInjection;

namespace LivroCaixa.App.Views;

public partial class LancamentosPageView : UserControl
{
    private readonly ExtratoViewModel _viewModel;

    public LancamentosPageView(ExtratoViewModel viewModel)
    {
        InitializeComponent();

        _viewModel = viewModel;
        DataContext = _viewModel;

        _viewModel.SolicitarNovoLancamento += async (_, _) => await AbrirFormularioAsync(null);
        _viewModel.SolicitarEdicaoLancamento += async (_, id) => await AbrirFormularioAsync(id);
        _viewModel.SolicitarNovaTransferencia += async (_, _) => await AbrirTransferenciaAsync();
    }

    private async Task AbrirTransferenciaAsync()
    {
        var transferenciaViewModel = App.Services.GetRequiredService<TransferenciaViewModelFactory>().Criar(_viewModel.EmpresaId);
        var janela = new TransferenciaWindow(transferenciaViewModel) { Owner = Window.GetWindow(this) };
        if (janela.ShowDialog() == true)
            await _viewModel.RecarregarAsync();
    }

    private async void BotaoExcluir_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: LancamentoResumoDto item })
            return;

        var confirmar = MessageBox.Show(
            item.EhTransferencia
                ? "Deseja realmente excluir esta transferência? Ela sai das duas contas."
                : "Deseja realmente excluir este lançamento?",
            "Confirmar exclusão",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (confirmar != MessageBoxResult.Yes)
            return;

        if (item.EhTransferencia)
            await _viewModel.ExcluirTransferenciaCommand.ExecuteAsync(item.Id);
        else
            await _viewModel.ExcluirLancamentoCommand.ExecuteAsync(item.Id);
    }

    private async void BotaoVer_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: Guid lancamentoId })
            return;

        await AbrirDetalhesAsync(lancamentoId);
    }

    private async void GradeLancamentos_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if ((e.OriginalSource as FrameworkElement)?.DataContext is LancamentoResumoDto { EhTransferencia: false } lancamento)
            await AbrirDetalhesAsync(lancamento.Id);
    }

    private async void GradeLancamentos_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Return)
            return;

        if (GradeLancamentos.SelectedItem is LancamentoResumoDto { EhTransferencia: false } lancamento)
        {
            await AbrirDetalhesAsync(lancamento.Id);
            e.Handled = true;
        }
    }

    private async Task AbrirDetalhesAsync(Guid lancamentoId)
    {
        var factory = App.Services.GetRequiredService<LancamentoDetalheViewModelFactory>();
        var detalheViewModel = await factory.CriarAsync(lancamentoId);

        var janela = new LancamentoDetalheWindow(detalheViewModel) { Owner = Window.GetWindow(this) };
        janela.ShowDialog();
    }

    private async Task AbrirFormularioAsync(Guid? lancamentoId)
    {
        var factory = App.Services.GetRequiredService<LancamentoFormViewModelFactory>();
        var formViewModel = lancamentoId is Guid id
            ? factory.CriarParaEdicao(_viewModel.EmpresaId, id)
            : factory.CriarParaNovo(_viewModel.EmpresaId);

        var janela = new LancamentoWindow(formViewModel) { Owner = Window.GetWindow(this) };
        janela.ShowDialog();

        // Recarrega sempre, mesmo se a janela foi fechada sem clicar em "Salvar": anexar um
        // comprovante já salva o lançamento em segundo plano (ver GarantirLancamentoCriadoAsync).
        await _viewModel.RecarregarAsync();
    }
}
