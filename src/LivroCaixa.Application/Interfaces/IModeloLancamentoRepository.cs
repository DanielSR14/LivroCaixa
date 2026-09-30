using LivroCaixa.Domain.Entities;

namespace LivroCaixa.Application.Interfaces;

public interface IModeloLancamentoRepository
{
    Task AdicionarAsync(ModeloLancamento modelo);

    Task<ModeloLancamento?> ObterPorIdAsync(Guid id);

    Task<List<ModeloLancamento>> ListarPorEmpresaAsync(Guid empresaId);

    void Remover(ModeloLancamento modelo);
}
