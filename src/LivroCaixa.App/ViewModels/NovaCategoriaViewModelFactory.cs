using LivroCaixa.Application.Services;
using LivroCaixa.Domain.Entities;

namespace LivroCaixa.App.ViewModels;

public class NovaCategoriaViewModelFactory
{
    private readonly CategoriaService _categoriaService;

    public NovaCategoriaViewModelFactory(CategoriaService categoriaService)
    {
        _categoriaService = categoriaService;
    }

    public NovaCategoriaViewModel CriarParaNovo(Guid empresaId) => new(_categoriaService, empresaId);

    public NovaCategoriaViewModel CriarParaEdicao(Guid empresaId, Categoria categoriaExistente) =>
        new(_categoriaService, empresaId, categoriaExistente);
}
