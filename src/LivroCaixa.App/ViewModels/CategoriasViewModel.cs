using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LivroCaixa.Application.Services;
using LivroCaixa.Domain.Entities;
using LivroCaixa.Domain.Exceptions;

namespace LivroCaixa.App.ViewModels;

public partial class CategoriasViewModel : ObservableObject
{
    private readonly CategoriaService _categoriaService;
    private readonly Guid _empresaId;

    public ObservableCollection<Categoria> Categorias { get; } = new();

    [ObservableProperty]
    private string mensagemErro = string.Empty;

    public event EventHandler? SolicitarNovaCategoria;

    public event EventHandler<Categoria>? SolicitarEditarCategoria;

    public IAsyncRelayCommand CarregarCommand { get; }

    public IRelayCommand NovaCategoriaCommand { get; }

    public IRelayCommand<Categoria> EditarCategoriaCommand { get; }

    public IAsyncRelayCommand<Categoria> ExcluirCategoriaCommand { get; }

    public CategoriasViewModel(CategoriaService categoriaService, Guid empresaId)
    {
        _categoriaService = categoriaService;
        _empresaId = empresaId;

        CarregarCommand = new AsyncRelayCommand(CarregarAsync);
        NovaCategoriaCommand = new RelayCommand(() => SolicitarNovaCategoria?.Invoke(this, EventArgs.Empty));
        EditarCategoriaCommand = new RelayCommand<Categoria>(categoria =>
        {
            if (categoria is not null)
                SolicitarEditarCategoria?.Invoke(this, categoria);
        });
        ExcluirCategoriaCommand = new AsyncRelayCommand<Categoria>(ExcluirAsync!);
    }

    public Task InicializarAsync() => CarregarAsync();

    private async Task CarregarAsync()
    {
        MensagemErro = string.Empty;
        try
        {
            var categorias = await _categoriaService.ListarAsync(_empresaId);
            Categorias.Clear();
            foreach (var categoria in categorias.OrderBy(c => c.Tipo).ThenBy(c => c.Nome))
                Categorias.Add(categoria);
        }
        catch (DomainException ex)
        {
            MensagemErro = ex.Message;
        }
    }

    private async Task ExcluirAsync(Categoria categoria)
    {
        MensagemErro = string.Empty;
        try
        {
            await _categoriaService.ExcluirAsync(categoria.Id);
            await CarregarAsync();
        }
        catch (DomainException ex)
        {
            MensagemErro = ex.Message;
        }
    }

    public Task RecarregarAsync() => CarregarAsync();
}
