using LivroCaixa.Application.Interfaces;
using LivroCaixa.Domain.Entities;
using LivroCaixa.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace LivroCaixa.Infrastructure.Repositories;

public class LancamentoRepository : ILancamentoRepository
{
    private readonly LivroCaixaDbContext _dbContext;

    public LancamentoRepository(LivroCaixaDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AdicionarAsync(Lancamento lancamento) => await _dbContext.Lancamentos.AddAsync(lancamento);

    public void RegistrarNovoAnexo(Anexo anexo) => _dbContext.Anexos.Add(anexo);

    public Task<Lancamento?> ObterPorIdAsync(Guid id) =>
        _dbContext.Lancamentos.Include(l => l.Anexos).SingleOrDefaultAsync(l => l.Id == id);

    // AsNoTracking é importante aqui: o DbContext vive o app inteiro (os serviços são resolvidos
    // uma vez pela MainWindow), e cada recarga do extrato rastreando todos os lançamentos +
    // anexos faria o change tracker crescer sem limite, deixando todo SaveChanges mais lento.
    public Task<List<Lancamento>> ListarPorEmpresaSomenteLeituraAsync(Guid empresaId, bool incluirExcluidos = false)
    {
        var query = _dbContext.Lancamentos.AsNoTracking().Include(l => l.Anexos).Where(l => l.EmpresaId == empresaId);
        if (!incluirExcluidos)
            query = query.Where(l => l.ExcluidoEm == null);

        return query.ToListAsync();
    }

    public void Remover(Lancamento lancamento) => _dbContext.Lancamentos.Remove(lancamento);

    public void RegistrarHistorico(HistoricoLancamento historico) => _dbContext.HistoricosLancamento.Add(historico);

    public Task<List<HistoricoLancamento>> ListarHistoricoPorLancamentoAsync(Guid lancamentoId) =>
        _dbContext.HistoricosLancamento.Where(h => h.LancamentoId == lancamentoId).ToListAsync();

    public Task<List<HistoricoLancamento>> ListarHistoricoPorPeriodoAsync(Guid empresaId, DateTime inicio, DateTime fim) =>
        _dbContext.HistoricosLancamento
            .Where(h => h.EmpresaId == empresaId && h.DataHora >= inicio && h.DataHora <= fim)
            .ToListAsync();

    public Task<bool> ExisteLancamentoParaContaAsync(Guid contaId) =>
        _dbContext.Lancamentos.AnyAsync(l => l.ContaId == contaId);

    public Task<bool> ExisteLancamentoParaCategoriaAsync(Guid categoriaId) =>
        _dbContext.Lancamentos.AnyAsync(l => l.CategoriaId == categoriaId);
}
