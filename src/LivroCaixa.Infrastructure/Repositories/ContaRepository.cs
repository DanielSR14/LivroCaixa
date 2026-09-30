using LivroCaixa.Application.Interfaces;
using LivroCaixa.Domain.Entities;
using LivroCaixa.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace LivroCaixa.Infrastructure.Repositories;

public class ContaRepository : IContaRepository
{
    private readonly LivroCaixaDbContext _dbContext;

    public ContaRepository(LivroCaixaDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AdicionarAsync(Conta conta) => await _dbContext.Contas.AddAsync(conta);

    public Task<Conta?> ObterPorIdAsync(Guid id) => _dbContext.Contas.SingleOrDefaultAsync(c => c.Id == id);

    public Task<List<Conta>> ListarPorEmpresaAsync(Guid empresaId) =>
        _dbContext.Contas.Where(c => c.EmpresaId == empresaId).ToListAsync();

    public void Remover(Conta conta) => _dbContext.Contas.Remove(conta);
}
