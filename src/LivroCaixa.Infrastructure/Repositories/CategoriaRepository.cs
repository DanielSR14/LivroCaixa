using LivroCaixa.Application.Interfaces;
using LivroCaixa.Domain.Entities;
using LivroCaixa.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace LivroCaixa.Infrastructure.Repositories;

public class CategoriaRepository : ICategoriaRepository
{
    private readonly LivroCaixaDbContext _dbContext;

    public CategoriaRepository(LivroCaixaDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AdicionarVariasAsync(IEnumerable<Categoria> categorias) =>
        await _dbContext.Categorias.AddRangeAsync(categorias);

    public Task<Categoria?> ObterPorIdAsync(Guid id) => _dbContext.Categorias.SingleOrDefaultAsync(c => c.Id == id);

    public Task<List<Categoria>> ListarPorEmpresaAsync(Guid empresaId) =>
        _dbContext.Categorias.Where(c => c.EmpresaId == empresaId).ToListAsync();

    public void Remover(Categoria categoria) => _dbContext.Categorias.Remove(categoria);
}
