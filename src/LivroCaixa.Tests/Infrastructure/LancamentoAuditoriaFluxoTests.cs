using LivroCaixa.Domain.Entities;
using LivroCaixa.Domain.Enums;
using LivroCaixa.Infrastructure.Database;
using LivroCaixa.Infrastructure.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace LivroCaixa.Tests.Infrastructure;

/// <summary>
/// Cobre a persistência real (SQLite) da exclusão lógica de Lancamento e do histórico de
/// auditoria — pega exatamente o tipo de bug que testes com fake não pegariam (ver armadilha #3
/// em CLAUDE.md: HistoricoLancamento é adicionado via RegistrarHistorico, não via navegação).
/// </summary>
public class LancamentoAuditoriaFluxoTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly LivroCaixaDbContext _context;
    private readonly Empresa _empresa;
    private readonly Conta _conta;
    private readonly Categoria _categoria;

    public LancamentoAuditoriaFluxoTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<LivroCaixaDbContext>().UseSqlite(_connection).Options;
        _context = new LivroCaixaDbContext(options);
        _context.Database.EnsureCreated();

        _empresa = new Empresa("Sindicato Teste", "12.345.678/0001-90");
        _conta = new Conta(_empresa.Id, "Caixa", TipoConta.Caixa, 1000m);
        _categoria = new Categoria(_empresa.Id, "Mensalidades", TipoLancamento.Entrada);

        _context.Empresas.Add(_empresa);
        _context.Contas.Add(_conta);
        _context.Categorias.Add(_categoria);
        _context.SaveChanges();
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    private Lancamento CriarLancamento()
    {
        var lancamento = new Lancamento(
            _empresa.Id, _conta, _categoria, TipoLancamento.Entrada,
            DateOnly.FromDateTime(DateTime.Today), 250m, FormaPagamento.Pix);
        _context.Lancamentos.Add(lancamento);
        _context.SaveChanges();
        return lancamento;
    }

    private LivroCaixaDbContext NovoContexto() =>
        new(new DbContextOptionsBuilder<LivroCaixaDbContext>().UseSqlite(_connection).Options);

    [Fact]
    public async Task Exclusao_logica_deve_persistir_e_sumir_da_listagem_ativa()
    {
        var lancamento = CriarLancamento();

        lancamento.Excluir();
        _context.SaveChanges();

        using var novoContexto = NovoContexto();
        var repositorio = new LancamentoRepository(novoContexto);

        var ativos = await repositorio.ListarPorEmpresaSomenteLeituraAsync(_empresa.Id);
        Assert.Empty(ativos);

        var todos = await repositorio.ListarPorEmpresaSomenteLeituraAsync(_empresa.Id, incluirExcluidos: true);
        var recarregado = Assert.Single(todos);
        Assert.NotNull(recarregado.ExcluidoEm);
    }

    [Fact]
    public async Task Historico_registrado_via_RegistrarHistorico_deve_persistir_sem_navegacao()
    {
        var lancamento = CriarLancamento();
        var repositorio = new LancamentoRepository(_context);

        // Este é exatamente o padrão da armadilha #3: HistoricoLancamento tem Id (Guid) próprio
        // atribuído no construtor, então precisa ser adicionado explicitamente (Set<T>().Add),
        // nunca só através de uma coleção de navegação do Lancamento (que nem existe aqui).
        repositorio.RegistrarHistorico(new HistoricoLancamento(_empresa.Id, lancamento.Id, TipoEventoHistorico.Criado, null));
        await _context.SaveChangesAsync();

        using var novoContexto = NovoContexto();
        var repositorioNovo = new LancamentoRepository(novoContexto);
        var historico = await repositorioNovo.ListarHistoricoPorLancamentoAsync(lancamento.Id);

        var evento = Assert.Single(historico);
        Assert.Equal(TipoEventoHistorico.Criado, evento.TipoEvento);
    }

    [Fact]
    public async Task ListarHistoricoPorPeriodo_deve_filtrar_por_data_do_evento_nao_do_lancamento()
    {
        var lancamentoAntigo = CriarLancamento();
        var repositorio = new LancamentoRepository(_context);

        // O evento "acontece" hoje, mesmo que o lançamento em si seja de uma competência antiga —
        // é exatamente o cenário descrito em IDEIAS_E_MELHORIAS.md, item 5 (correção tardia).
        var inicioDoMesAtual = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var fimDoMesAtual = inicioDoMesAtual.AddMonths(1).AddTicks(-1);

        repositorio.RegistrarHistorico(new HistoricoLancamento(_empresa.Id, lancamentoAntigo.Id, TipoEventoHistorico.Editado, "{}"));
        await _context.SaveChangesAsync();

        var eventosDoMes = await repositorio.ListarHistoricoPorPeriodoAsync(_empresa.Id, inicioDoMesAtual, fimDoMesAtual);
        Assert.Single(eventosDoMes);

        var eventosDoMesPassado = await repositorio.ListarHistoricoPorPeriodoAsync(
            _empresa.Id, inicioDoMesAtual.AddMonths(-1), inicioDoMesAtual.AddTicks(-1));
        Assert.Empty(eventosDoMesPassado);
    }
}
