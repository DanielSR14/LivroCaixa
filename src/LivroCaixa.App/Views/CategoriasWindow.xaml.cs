using System.Windows;
using System.Windows.Controls;
using LivroCaixa.App.ViewModels;
using LivroCaixa.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace LivroCaixa.App.Views;

public partial class CategoriasWindow : Window
{
    private readonly CategoriasViewModel _viewModel;
    private readonly Guid _empresaId;

    public CategoriasWindow(CategoriasViewModel viewModel, Guid empresaId)
    {
        InitializeComponent();

        _viewModel = viewModel;
        _empresaId = empresaId;
        DataContext = _viewModel;

        _viewModel.SolicitarNovaCategoria += async (_, _) => await AbrirNovaCategoriaAsync();
        _viewModel.SolicitarEditarCategoria += async (_, categoria) => await AbrirEditarCategoriaAsync(categoria);
        Loaded += async (_, _) => await _viewModel.InicializarAsync();
    }

    private async Task AbrirNovaCategoriaAsync()
    {
        var factory = App.Services.GetRequiredService<NovaCategoriaViewModelFactory>();
        var novaCategoriaViewModel = factory.CriarParaNovo(_empresaId);

        var janela = new NovaCategoriaWindow(novaCategoriaViewModel) { Owner = this };
        if (janela.ShowDialog() == true)
            await _viewModel.RecarregarAsync();
    }

    private async Task AbrirEditarCategoriaAsync(Categoria categoria)
    {
        var factory = App.Services.GetRequiredService<NovaCategoriaViewModelFactory>();
        var editarCategoriaViewModel = factory.CriarParaEdicao(_empresaId, categoria);

        var janela = new NovaCategoriaWindow(editarCategoriaViewModel) { Owner = this };
        if (janela.ShowDialog() == true)
            await _viewModel.RecarregarAsync();
    }

    private async void BotaoExcluir_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: Categoria categoria })
            return;

        var confirmar = MessageBox.Show(
            $"Deseja realmente excluir a categoria \"{categoria.Nome}\"?",
            "Confirmar exclusão",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (confirmar == MessageBoxResult.Yes)
            await _viewModel.ExcluirCategoriaCommand.ExecuteAsync(categoria);
    }
}
