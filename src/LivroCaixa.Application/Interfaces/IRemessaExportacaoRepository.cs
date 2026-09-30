using LivroCaixa.Domain.Entities;

namespace LivroCaixa.Application.Interfaces;

public interface IRemessaExportacaoRepository
{
    Task AdicionarAsync(RemessaExportacao remessa);

    Task<List<RemessaExportacao>> ListarPorEmpresaAsync(Guid empresaId);
}
