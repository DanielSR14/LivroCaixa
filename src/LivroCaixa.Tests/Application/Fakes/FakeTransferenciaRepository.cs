using LivroCaixa.Application.Interfaces;
using LivroCaixa.Domain.Entities;

namespace LivroCaixa.Tests.Application.Fakes;

public class FakeTransferenciaRepository : ITransferenciaRepository
{
    public List<Transferencia> Transferencias { get; } = new();

    public Task AdicionarAsync(Transferencia transferencia)
    {
        Transferencias.Add(transferencia);
        return Task.CompletedTask;
    }

    public Task<Transferencia?> ObterPorIdAsync(Guid id) => Task.FromResult(Transferencias.SingleOrDefault(t => t.Id == id));

    public Task<List<Transferencia>> ListarPorEmpresaSomenteLeituraAsync(Guid empresaId) =>
        Task.FromResult(Transferencias.Where(t => t.EmpresaId == empresaId && t.ExcluidoEm == null).ToList());

    public Task<bool> ExisteTransferenciaParaContaAsync(Guid contaId) =>
        Task.FromResult(Transferencias.Any(t => t.ContaOrigemId == contaId || t.ContaDestinoId == contaId));
}
