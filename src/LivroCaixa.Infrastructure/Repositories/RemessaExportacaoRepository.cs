using LivroCaixa.Application.Interfaces;
using LivroCaixa.Domain.Entities;
using LivroCaixa.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace LivroCaixa.Infrastructure.Repositories;

public class RemessaExportacaoRepository : IRemessaExportacaoRepository
{
    private readonly LivroCaixaDbContext _dbContext;

    public RemessaExportacaoRepository(LivroCaixaDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AdicionarAsync(RemessaExportacao remessa) => await _dbContext.RemessasExportacao.AddAsync(remessa);

    public Task<List<RemessaExportacao>> ListarPorEmpresaAsync(Guid empresaId) =>
        _dbContext.RemessasExportacao.Where(r => r.EmpresaId == empresaId).ToListAsync();
}
