using LivroCaixa.Application.Services;
using LivroCaixa.Domain.Entities;
using LivroCaixa.Domain.Enums;
using LivroCaixa.Infrastructure.Database;
using LivroCaixa.Infrastructure.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace LivroCaixa.Tests.Infrastructure;

public class RelatorioServiceFluxoTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly LivroCaixaDbContext _context;
    private readonly LancamentoRepository _lancamentoRepository;
    private readonly ContaRepository _contaRepository;
    private readonly RelatorioService _relatorioService;

    private readonly Empresa _empresa;
    private readonly Conta _conta;
    private readonly Categoria _categoriaVendas;
    private readonly Categoria _categoriaAluguel;

    public RelatorioServiceFluxoTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<LivroCaixaDbContext>().UseSqlite(_connection).Options;
        _context = new LivroCaixaDbContext(options);
        _context.Database.EnsureCreated();

        _contaRepository = new ContaRepository(_context);
        _lancamentoRepository = new LancamentoRepository(_context);
        var categoriaRepository = new CategoriaRepository(_context);
        var lancamentoService = new LancamentoService(_lancamentoRepository, _contaRepository, categoriaRepository, new TransferenciaRepository(_context), _context);
        _relatorioService = new RelatorioService(lancamentoService, _contaRepository, _lancamentoRepository);

        _empresa = new Empresa("Sindicato dos Metalúrgicos", "12.345.678/0001-90");
        _conta = new Conta(_empresa.Id, "Caixa", TipoConta.Caixa, 1000m);
        _categoriaVendas = new Categoria(_empresa.Id, "Mensalidades", TipoLancamento.Entrada);
        _categoriaAluguel = new Categoria(_empresa.Id, "Aluguel", TipoLancamento.Saida);

        _context.Empresas.Add(_empresa);
        _context.Contas.Add(_conta);
        _context.Categorias.AddRange(_categoriaVendas, _categoriaAluguel);
        _context.SaveChanges();
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    private void CriarLancamento(DateOnly data, decimal valor, TipoLancamento tipo, Categoria categoria)
    {
        var lancamento = new Lancamento(_empresa.Id, _conta, categoria, tipo, data, valor, FormaPagamento.Pix);
        _context.Lancamentos.Add(lancamento);
        _context.SaveChanges();
    }

    [Fact]
    public async Task Deve_gerar_relatorio_do_periodo_com_totais_e_categorias()
    {
        CriarLancamento(new DateOnly(2026, 7, 5), 500m, TipoLancamento.Entrada, _categoriaVendas);
        CriarLancamento(new DateOnly(2026, 7, 10), 300m, TipoLancamento.Entrada, _categoriaVendas);
        CriarLancamento(new DateOnly(2026, 7, 15), 200m, TipoLancamento.Saida, _categoriaAluguel);
        // Fora do período — não deve entrar no relatório de julho.
        CriarLancamento(new DateOnly(2026, 8, 1), 999m, TipoLancamento.Entrada, _categoriaVendas);

        var relatorio = await _relatorioService.GerarRelatorioPeriodoAsync(_empresa.Id, 2026, 7);

        Assert.Equal(800m, relatorio.TotalEntradas);
        Assert.Equal(200m, relatorio.TotalSaidas);
        Assert.Equal(600m, relatorio.Resultado);
        Assert.Equal(3, relatorio.Extrato.Count);

        var entradaVendas = Assert.Single(relatorio.EntradasPorCategoria);
        Assert.Equal("Mensalidades", entradaVendas.Categoria);
        Assert.Equal(800m, entradaVendas.Total);

        var saidaAluguel = Assert.Single(relatorio.SaidasPorCategoria);
        Assert.Equal("Aluguel", saidaAluguel.Categoria);
        Assert.Equal(200m, saidaAluguel.Total);
    }

    [Fact]
    public async Task Deve_filtrar_relatorio_de_periodo_por_conta_quando_informado()
    {
        var outraConta = new Conta(_empresa.Id, "Banco", TipoConta.Banco);
        _context.Contas.Add(outraConta);
        _context.SaveChanges();

        var lancamentoCaixa = new Lancamento(_empresa.Id, _conta, _categoriaVendas, TipoLancamento.Entrada,
            new DateOnly(2026, 7, 5), 500m, FormaPagamento.Pix);
        var lancamentoBanco = new Lancamento(_empresa.Id, outraConta, _categoriaVendas, TipoLancamento.Entrada,
            new DateOnly(2026, 7, 6), 900m, FormaPagamento.Pix);
        _context.Lancamentos.AddRange(lancamentoCaixa, lancamentoBanco);
        _context.SaveChanges();

        var relatorioSoBanco = await _relatorioService.GerarRelatorioPeriodoAsync(_empresa.Id, 2026, 7, outraConta.Id);

        Assert.Equal(900m, relatorioSoBanco.TotalEntradas);
        var extratoDoBanco = Assert.Single(relatorioSoBanco.Extrato);
        Assert.Equal("Banco", extratoDoBanco.Conta);
    }

    [Fact]
    public async Task Relatorio_de_periodo_sem_lancamentos_deve_vir_zerado()
    {
        var relatorio = await _relatorioService.GerarRelatorioPeriodoAsync(_empresa.Id, 2020, 1);

        Assert.Equal(0m, relatorio.TotalEntradas);
        Assert.Equal(0m, relatorio.TotalSaidas);
        Assert.Empty(relatorio.Extrato);
        Assert.Empty(relatorio.EntradasPorCategoria);
        Assert.Empty(relatorio.SaidasPorCategoria);
    }

    [Fact]
    public async Task Deve_calcular_saldo_acumulado_refletindo_lancamentos_ate_cada_mes()
    {
        var hoje = DateTime.Today;
        var doisMesesAtras = new DateOnly(hoje.Year, hoje.Month, 1).AddMonths(-2);
        var umMesAtras = new DateOnly(hoje.Year, hoje.Month, 1).AddMonths(-1);

        // Saldo inicial da conta = 1000. Entrada de 500 há 2 meses, saída de 200 há 1 mês.
        CriarLancamento(doisMesesAtras, 500m, TipoLancamento.Entrada, _categoriaVendas);
        CriarLancamento(umMesAtras, 200m, TipoLancamento.Saida, _categoriaAluguel);

        var saldos = await _relatorioService.ObterSaldoAcumuladoUltimosMesesAsync(_empresa.Id, quantidadeMeses: 3);

        Assert.Equal(3, saldos.Count);
        Assert.Equal(1500m, saldos[0].SaldoFinal); // fim do mês da entrada (2 meses atrás)
        Assert.Equal(1300m, saldos[1].SaldoFinal); // fim do mês da saída (1 mês atrás)
        Assert.Equal(1300m, saldos[2].SaldoFinal); // mês atual, sem novas movimentações
    }

    [Fact]
    public async Task Saldo_acumulado_nao_deve_projetar_saldo_inicial_para_meses_anteriores_ao_primeiro_lancamento()
    {
        // Empresa nova: só existe um lançamento neste mês. Os outros 5 meses da janela de
        // "últimos 6 meses" não devem aparecer com o saldo inicial (1000) repetido como se a
        // conta já existisse e estivesse parada naqueles meses — eles simplesmente não existiram
        // para efeito de uso do app.
        CriarLancamento(DateOnly.FromDateTime(DateTime.Today), 300m, TipoLancamento.Entrada, _categoriaVendas);

        var saldos = await _relatorioService.ObterSaldoAcumuladoUltimosMesesAsync(_empresa.Id, quantidadeMeses: 6);

        var mesAtual = Assert.Single(saldos);
        Assert.Equal(DateTime.Today.Month, mesAtual.Mes);
        Assert.Equal(DateTime.Today.Year, mesAtual.Ano);
        Assert.Equal(1300m, mesAtual.SaldoFinal);
    }
}
