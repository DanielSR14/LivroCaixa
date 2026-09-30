using LivroCaixa.Application.Services;
using LivroCaixa.Domain.Entities;
using LivroCaixa.Domain.Enums;
using LivroCaixa.Infrastructure.Database;
using LivroCaixa.Infrastructure.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace LivroCaixa.Tests.Infrastructure;

/// <summary>
/// Cobre a leitura consolidada por empresa (uma consulta só, sem rastreamento) que substituiu
/// o laço "uma consulta por conta" no extrato, relatórios, contas, exportação e armazenamento.
/// </summary>
public class ExtratoConsolidadoFluxoTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly LivroCaixaDbContext _context;
    private readonly LancamentoRepository _lancamentoRepository;
    private readonly LancamentoService _lancamentoService;

    private readonly Empresa _empresa;
    private readonly Conta _caixa;
    private readonly Conta _banco;
    private readonly Categoria _mensalidades;
    private readonly Categoria _aluguel;

    public ExtratoConsolidadoFluxoTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<LivroCaixaDbContext>().UseSqlite(_connection).Options;
        _context = new LivroCaixaDbContext(options);
        _context.Database.EnsureCreated();

        _lancamentoRepository = new LancamentoRepository(_context);
        _lancamentoService = new LancamentoService(
            _lancamentoRepository, new ContaRepository(_context), new CategoriaRepository(_context), new TransferenciaRepository(_context), _context);

        _empresa = new Empresa("Sindicato dos Metalúrgicos", "12.345.678/0001-90");
        _caixa = new Conta(_empresa.Id, "Caixa", TipoConta.Caixa, 100m);
        _banco = new Conta(_empresa.Id, "Sicoob", TipoConta.Banco, 1000m);
        _mensalidades = new Categoria(_empresa.Id, "Mensalidades", TipoLancamento.Entrada);
        _aluguel = new Categoria(_empresa.Id, "Aluguel", TipoLancamento.Saida);

        _context.Empresas.Add(_empresa);
        _context.Contas.AddRange(_caixa, _banco);
        _context.Categorias.AddRange(_mensalidades, _aluguel);
        _context.SaveChanges();
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    private Lancamento CriarLancamento(Conta conta, Categoria categoria, DateOnly data, decimal valor)
    {
        var lancamento = new Lancamento(_empresa.Id, conta, categoria, categoria.Tipo, data, valor, FormaPagamento.Pix);
        _context.Lancamentos.Add(lancamento);
        _context.SaveChanges();
        return lancamento;
    }

    [Fact]
    public async Task Deve_calcular_saldo_por_conta_e_saldo_total_na_mesma_leitura()
    {
        CriarLancamento(_caixa, _mensalidades, new DateOnly(2026, 9, 1), 50m);
        CriarLancamento(_banco, _aluguel, new DateOnly(2026, 9, 2), 300m);
        CriarLancamento(_caixa, _aluguel, new DateOnly(2026, 9, 3), 20m);
        var excluido = CriarLancamento(_banco, _mensalidades, new DateOnly(2026, 9, 4), 999m);
        excluido.Excluir();
        _context.SaveChanges();

        var extrato = await _lancamentoService.ObterExtratoConsolidadoAsync(_empresa.Id);

        // Caixa: 100 + 50 - 20 = 130; Sicoob: 1000 - 300 = 700. O excluído não entra em nada.
        Assert.Equal(830m, extrato.SaldoTotal);
        Assert.Equal(3, extrato.Lancamentos.Count);
        Assert.Equal(130m, extrato.Lancamentos.First(l => l.Conta == "Caixa").SaldoAposLancamento);
        Assert.Equal(700m, extrato.Lancamentos.Single(l => l.Conta == "Sicoob").SaldoAposLancamento);
        Assert.Equal(extrato.SaldoTotal, await _lancamentoService.ObterSaldoTotalAsync(_empresa.Id));

        var excluidos = await _lancamentoService.ListarExcluidosAsync(_empresa.Id);
        Assert.Equal(excluido.Id, Assert.Single(excluidos).Id);
    }

    [Fact]
    public async Task Leitura_por_empresa_nao_deve_rastrear_entidades_nem_trazer_outra_empresa()
    {
        var lancamento = CriarLancamento(_caixa, _mensalidades, new DateOnly(2026, 9, 1), 50m);
        var anexo = new Anexo(lancamento.Id, "x/comprovante.pdf", "comprovante.pdf", TipoArquivoAnexo.Pdf, 1024);
        lancamento.AdicionarAnexo(anexo);
        _lancamentoRepository.RegistrarNovoAnexo(anexo);

        var outraEmpresa = new Empresa("Associação de Moradores", "98.765.432/0001-10");
        var contaOutra = new Conta(outraEmpresa.Id, "Caixa", TipoConta.Caixa, 0m);
        var categoriaOutra = new Categoria(outraEmpresa.Id, "Doações", TipoLancamento.Entrada);
        _context.AddRange(outraEmpresa, contaOutra, categoriaOutra);
        _context.Lancamentos.Add(new Lancamento(
            outraEmpresa.Id, contaOutra, categoriaOutra, TipoLancamento.Entrada, new DateOnly(2026, 9, 1), 10m, FormaPagamento.Pix));
        _context.SaveChanges();
        _context.ChangeTracker.Clear();

        var lancamentos = await _lancamentoRepository.ListarPorEmpresaSomenteLeituraAsync(_empresa.Id);

        var unico = Assert.Single(lancamentos);
        Assert.Single(unico.Anexos);
        Assert.Empty(_context.ChangeTracker.Entries());
    }
}
