using LivroCaixa.Application.Interfaces;
using LivroCaixa.Domain.Entities;

namespace LivroCaixa.Tests.Application.Fakes;

public class FakeCategoriaRepository : ICategoriaRepository
{
    public List<Categoria> Categorias { get; } = new();

    public Task AdicionarVariasAsync(IEnumerable<Categoria> categorias)
    {
        Categorias.AddRange(categorias);
        return Task.CompletedTask;
    }

    public Task<Categoria?> ObterPorIdAsync(Guid id) => Task.FromResult(Categorias.SingleOrDefault(c => c.Id == id));

    public Task<List<Categoria>> ListarPorEmpresaAsync(Guid empresaId) =>
        Task.FromResult(Categorias.Where(c => c.EmpresaId == empresaId).ToList());

    public void Remover(Categoria categoria) => Categorias.Remove(categoria);
}
