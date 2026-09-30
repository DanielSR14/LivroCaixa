using LivroCaixa.Domain.Entities;

namespace LivroCaixa.Application.Interfaces;

public interface IContaRepository
{
    Task AdicionarAsync(Conta conta);

    Task<Conta?> ObterPorIdAsync(Guid id);

    Task<List<Conta>> ListarPorEmpresaAsync(Guid empresaId);

    void Remover(Conta conta);
}
