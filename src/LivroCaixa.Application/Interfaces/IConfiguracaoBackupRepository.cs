using LivroCaixa.Domain.Entities;

namespace LivroCaixa.Application.Interfaces;

public interface IConfiguracaoBackupRepository
{
    Task<ConfiguracaoBackup?> ObterPorEmpresaAsync(Guid empresaId);

    Task AdicionarAsync(ConfiguracaoBackup configuracao);
}
