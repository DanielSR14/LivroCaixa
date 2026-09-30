using System.Windows;
using System.Windows.Controls;
using LivroCaixa.App.ViewModels;
using LivroCaixa.Application.DTOs;
using Microsoft.Extensions.DependencyInjection;

namespace LivroCaixa.App.Views;

public partial class ModelosWindow : Window
{
    private readonly ModelosViewModel _viewModel;
    private readonly Guid _empresaId;

    public ModelosWindow(ModelosViewModel viewModel, Guid empresaId)
    {
        InitializeComponent();

        _viewModel = viewModel;
        _empresaId = empresaId;
        DataContext = _viewModel;

        _viewModel.SolicitarNovoModelo += async (_, _) =>
            await AbrirFormularioAsync(App.Services.GetRequiredService<ModeloFormViewModelFactory>().CriarParaNovo(_empresaId));
        _viewModel.SolicitarEditarModelo += async (_, modelo) =>
            await AbrirFormularioAsync(App.Services.GetRequiredService<ModeloFormViewModelFactory>().CriarParaEdicao(_empresaId, modelo));
        Loaded += async (_, _) => await _viewModel.CarregarAsync();
    }

    private async Task AbrirFormularioAsync(ModeloFormViewModel formViewModel)
    {
        if (new ModeloFormWindow(formViewModel) { Owner = this }.ShowDialog() == true)
            await _viewModel.CarregarAsync();
    }

    private async void BotaoExcluir_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: ModeloLancamentoDto modelo })
            return;

        var confirmar = MessageBox.Show(
            $"Deseja realmente excluir o modelo \"{modelo.Exibicao}\"? Os lançamentos já feitos com ele não são afetados.",
            "Confirmar exclusão",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (confirmar == MessageBoxResult.Yes)
            await _viewModel.ExcluirModeloCommand.ExecuteAsync(modelo);
    }
}
