using LivroCaixa.Application.Interfaces;
using LivroCaixa.Domain.Entities;

namespace LivroCaixa.Tests.Application.Fakes;

public class FakeContaRepository : IContaRepository
{
    public List<Conta> Contas { get; } = new();

    public Task AdicionarAsync(Conta conta)
    {
        Contas.Add(conta);
        return Task.CompletedTask;
    }

    public Task<Conta?> ObterPorIdAsync(Guid id) => Task.FromResult(Contas.SingleOrDefault(c => c.Id == id));

    public Task<List<Conta>> ListarPorEmpresaAsync(Guid empresaId) =>
        Task.FromResult(Contas.Where(c => c.EmpresaId == empresaId).ToList());

    public void Remover(Conta conta) => Contas.Remove(conta);
}
