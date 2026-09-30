using LivroCaixa.Domain.Entities;

namespace LivroCaixa.Application.Interfaces;

public interface ICategoriaRepository
{
    Task AdicionarVariasAsync(IEnumerable<Categoria> categorias);

    Task<Categoria?> ObterPorIdAsync(Guid id);

    Task<List<Categoria>> ListarPorEmpresaAsync(Guid empresaId);

    void Remover(Categoria categoria);
}
