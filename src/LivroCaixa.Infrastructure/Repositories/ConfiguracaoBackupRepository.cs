using LivroCaixa.Application.Interfaces;
using LivroCaixa.Domain.Entities;
using LivroCaixa.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace LivroCaixa.Infrastructure.Repositories;

public class ConfiguracaoBackupRepository : IConfiguracaoBackupRepository
{
    private readonly LivroCaixaDbContext _dbContext;

    public ConfiguracaoBackupRepository(LivroCaixaDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<ConfiguracaoBackup?> ObterPorEmpresaAsync(Guid empresaId) =>
        _dbContext.ConfiguracoesBackup.SingleOrDefaultAsync(c => c.EmpresaId == empresaId);

    public async Task AdicionarAsync(ConfiguracaoBackup configuracao) => await _dbContext.ConfiguracoesBackup.AddAsync(configuracao);
}
