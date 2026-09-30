using LivroCaixa.Application.Interfaces;
using LivroCaixa.Domain.Entities;
using LivroCaixa.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace LivroCaixa.Infrastructure.Repositories;

public class ModeloLancamentoRepository : IModeloLancamentoRepository
{
    private readonly LivroCaixaDbContext _dbContext;

    public ModeloLancamentoRepository(LivroCaixaDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AdicionarAsync(ModeloLancamento modelo) => await _dbContext.ModelosLancamento.AddAsync(modelo);

    public Task<ModeloLancamento?> ObterPorIdAsync(Guid id) =>
        _dbContext.ModelosLancamento.SingleOrDefaultAsync(m => m.Id == id);

    public Task<List<ModeloLancamento>> ListarPorEmpresaAsync(Guid empresaId) =>
        _dbContext.ModelosLancamento.Where(m => m.EmpresaId == empresaId).ToListAsync();

    public void Remover(ModeloLancamento modelo) => _dbContext.ModelosLancamento.Remove(modelo);
}
