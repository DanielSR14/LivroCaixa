using LivroCaixa.Application.DTOs;
using LivroCaixa.Application.Interfaces;
using LivroCaixa.Domain.Enums;

namespace LivroCaixa.Application.Services;

/// <summary>
/// Relatórios do MVP1: resultado do período (entradas/saídas/resultado + por categoria),
/// extrato estilo bancário do período, e saldo acumulado mês a mês (fluxo de caixa).
/// </summary>
public class RelatorioService
{
    private readonly LancamentoService _lancamentoService;
    private readonly IContaRepository _contaRepository;
    private readonly ILancamentoRepository _lancamentoRepository;

    public RelatorioService(
        LancamentoService lancamentoService,
        IContaRepository contaRepository,
        ILancamentoRepository lancamentoRepository)
    {
        _lancamentoService = lancamentoService;
        _contaRepository = contaRepository;
        _lancamentoRepository = lancamentoRepository;
    }

    public async Task<RelatorioPeriodoDto> GerarRelatorioPeriodoAsync(Guid empresaId, int ano, int mes, Guid? contaId = null)
    {
        var extratoCompleto = await _lancamentoService.ListarExtratoConsolidadoAsync(empresaId);
        var extratoDoPeriodo = extratoCompleto
            .Where(l => l.Data.Year == ano && l.Data.Month == mes)
            .Where(l => contaId is null || l.ContaId == contaId)
            .OrderBy(l => l.Data)
            .ToList();

        // O extrato do período mostra as transferências (como um extrato bancário), mas elas não
        // entram no resultado: não são receita nem despesa.
        var doResultado = extratoDoPeriodo.Where(l => l.ContaNoResultado).ToList();
        var totalEntradas = doResultado.Where(l => l.Tipo == TipoLancamento.Entrada).Sum(l => l.Valor);
        var totalSaidas = doResultado.Where(l => l.Tipo == TipoLancamento.Saida).Sum(l => l.Valor);

        var entradasPorCategoria = AgruparPorCategoria(doResultado, TipoLancamento.Entrada);
        var saidasPorCategoria = AgruparPorCategoria(doResultado, TipoLancamento.Saida);

        return new RelatorioPeriodoDto(
            ano, mes, totalEntradas, totalSaidas, totalEntradas - totalSaidas,
            entradasPorCategoria, saidasPorCategoria, extratoDoPeriodo);
    }

    /// <summary>
    /// Saldo combinado de todas as contas ao final de cada um dos últimos <paramref name="quantidadeMeses"/>
    /// meses (incluindo o atual) — a base do gráfico de "fluxo de caixa acumulado".
    /// </summary>
    public async Task<IReadOnlyList<SaldoMensalDto>> ObterSaldoAcumuladoUltimosMesesAsync(Guid empresaId, int quantidadeMeses = 6)
    {
        var contas = await _contaRepository.ListarPorEmpresaAsync(empresaId);
        var saldoInicialTotal = contas.Sum(c => c.SaldoInicial);

        var idsContas = contas.Select(c => c.Id).ToHashSet();
        var movimentacoes = (await _lancamentoRepository.ListarPorEmpresaSomenteLeituraAsync(empresaId))
            .Where(l => idsContas.Contains(l.ContaId))
            .Select(l => (l.Data, Valor: l.Tipo == TipoLancamento.Entrada ? l.Valor : -l.Valor))
            .ToList();

        // A janela de "últimos N meses" não pode voltar antes do primeiro lançamento já registrado:
        // sem isso, o saldo inicial da conta (que só passou a valer a partir de quando ela foi
        // cadastrada) é projetado para trás como se já existisse naqueles meses, fazendo o gráfico
        // mostrar o mesmo valor "fixo" em todo mês sem movimentação anterior ao início real de uso do app.
        var primeiroDiaMesAtual = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        var inicioDaJanela = primeiroDiaMesAtual.AddMonths(-(quantidadeMeses - 1));
        var primeiroMesComDados = movimentacoes.Count > 0
            ? PrimeiroDiaDoMes(movimentacoes.Min(m => m.Data))
            : primeiroDiaMesAtual;
        var inicioReal = primeiroMesComDados > inicioDaJanela ? primeiroMesComDados : inicioDaJanela;

        var meses = new List<DateTime>();
        for (var mes = inicioReal; mes <= primeiroDiaMesAtual; mes = mes.AddMonths(1))
            meses.Add(mes);

        // Soma acumulada numa passada só pelas movimentações ordenadas por data, em vez de
        // re-somar a lista inteira a cada mês.
        var ordenadas = movimentacoes.OrderBy(m => m.Data).ToList();
        var indice = 0;
        var saldo = saldoInicialTotal;

        var resultado = new List<SaldoMensalDto>();
        foreach (var mes in meses)
        {
            var fimDoMes = new DateOnly(mes.Year, mes.Month, DateTime.DaysInMonth(mes.Year, mes.Month));
            while (indice < ordenadas.Count && ordenadas[indice].Data <= fimDoMes)
                saldo += ordenadas[indice++].Valor;

            resultado.Add(new SaldoMensalDto(mes.Year, mes.Month, saldo));
        }

        return resultado;
    }

    private static DateTime PrimeiroDiaDoMes(DateOnly data) => new(data.Year, data.Month, 1);

    private static List<ResultadoCategoriaDto> AgruparPorCategoria(IReadOnlyList<LancamentoResumoDto> lancamentos, TipoLancamento tipo) =>
        lancamentos
            .Where(l => l.Tipo == tipo)
            .GroupBy(l => l.Categoria)
            .Select(g => new ResultadoCategoriaDto(g.Key, g.Sum(l => l.Valor)))
            .OrderByDescending(r => r.Total)
            .ToList();
}
