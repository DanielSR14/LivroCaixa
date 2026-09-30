using LivroCaixa.Application.Interfaces;
using LivroCaixa.Domain.Entities;

namespace LivroCaixa.Tests.Application.Fakes;

public class FakeLancamentoRepository : ILancamentoRepository
{
    public List<Lancamento> Lancamentos { get; } = new();

    public List<HistoricoLancamento> Historico { get; } = new();

    public Task AdicionarAsync(Lancamento lancamento)
    {
        Lancamentos.Add(lancamento);
        return Task.CompletedTask;
    }

    public void RegistrarNovoAnexo(Anexo anexo)
    {
        // Fake em memória: o anexo já fica acessível via Lancamento.Anexos, nada a fazer aqui.
    }

    public Task<Lancamento?> ObterPorIdAsync(Guid id) => Task.FromResult(Lancamentos.SingleOrDefault(l => l.Id == id));

    public Task<List<Lancamento>> ListarPorEmpresaSomenteLeituraAsync(Guid empresaId, bool incluirExcluidos = false) =>
        Task.FromResult(Lancamentos
            .Where(l => l.EmpresaId == empresaId && (incluirExcluidos || l.ExcluidoEm == null))
            .ToList());

    public void Remover(Lancamento lancamento) => Lancamentos.Remove(lancamento);

    public void RegistrarHistorico(HistoricoLancamento historico) => Historico.Add(historico);

    public Task<List<HistoricoLancamento>> ListarHistoricoPorLancamentoAsync(Guid lancamentoId) =>
        Task.FromResult(Historico.Where(h => h.LancamentoId == lancamentoId).ToList());

    public Task<List<HistoricoLancamento>> ListarHistoricoPorPeriodoAsync(Guid empresaId, DateTime inicio, DateTime fim) =>
        Task.FromResult(Historico.Where(h => h.EmpresaId == empresaId && h.DataHora >= inicio && h.DataHora <= fim).ToList());

    public Task<bool> ExisteLancamentoParaContaAsync(Guid contaId) =>
        Task.FromResult(Lancamentos.Any(l => l.ContaId == contaId));

    public Task<bool> ExisteLancamentoParaCategoriaAsync(Guid categoriaId) =>
        Task.FromResult(Lancamentos.Any(l => l.CategoriaId == categoriaId));
}
