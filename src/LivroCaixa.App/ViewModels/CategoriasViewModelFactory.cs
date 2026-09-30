using LivroCaixa.Application.Services;

namespace LivroCaixa.App.ViewModels;

public class CategoriasViewModelFactory
{
    private readonly CategoriaService _categoriaService;

    public CategoriasViewModelFactory(CategoriaService categoriaService)
    {
        _categoriaService = categoriaService;
    }

    public CategoriasViewModel Criar(Guid empresaId) => new(_categoriaService, empresaId);
}
