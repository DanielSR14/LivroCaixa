using LivroCaixa.Application.Interfaces;
using LivroCaixa.Domain.Entities;
using LivroCaixa.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace LivroCaixa.Infrastructure.Repositories;

public class TransferenciaRepository : ITransferenciaRepository
{
    private readonly LivroCaixaDbContext _dbContext;

    public TransferenciaRepository(LivroCaixaDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AdicionarAsync(Transferencia transferencia) => await _dbContext.Transferencias.AddAsync(transferencia);

    public Task<Transferencia?> ObterPorIdAsync(Guid id) =>
        _dbContext.Transferencias.SingleOrDefaultAsync(t => t.Id == id);

    public Task<List<Transferencia>> ListarPorEmpresaSomenteLeituraAsync(Guid empresaId) =>
        _dbContext.Transferencias.AsNoTracking()
            .Where(t => t.EmpresaId == empresaId && t.ExcluidoEm == null)
            .ToListAsync();

    public Task<bool> ExisteTransferenciaParaContaAsync(Guid contaId) =>
        _dbContext.Transferencias.AnyAsync(t => t.ContaOrigemId == contaId || t.ContaDestinoId == contaId);
}
