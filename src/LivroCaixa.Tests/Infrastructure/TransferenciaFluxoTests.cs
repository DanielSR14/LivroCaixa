using System.IO.Compression;
using LivroCaixa.Application.DTOs;
using LivroCaixa.Application.Services;
using LivroCaixa.Domain.Entities;
using LivroCaixa.Domain.Enums;
using LivroCaixa.Domain.Exceptions;
using LivroCaixa.Infrastructure.Database;
using LivroCaixa.Infrastructure.Export;
using LivroCaixa.Infrastructure.Files;
using LivroCaixa.Infrastructure.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace LivroCaixa.Tests.Infrastructure;

/// <summary>
/// Transferência de ponta a ponta: muda o saldo de cada conta, aparece no extrato das duas, mas não
/// infla entradas/saídas no relatório e vai para transferencias.json (não lancamentos.json) na exportação.
/// </summary>
public class TransferenciaFluxoTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly LivroCaixaDbContext _context;
    private readonly string _diretorioDestino = Path.Combine(Path.GetTempPath(), $"contabil_transf_{Guid.NewGuid():N}");
    private readonly Empresa _empresa;
    private readonly Conta _banco;
    private readonly Conta _caixa;
    private readonly DateOnly _hoje = DateOnly.FromDateTime(DateTime.Today);

    public TransferenciaFluxoTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        _context = new LivroCaixaDbContext(new DbContextOptionsBuilder<LivroCaixaDbContext>().UseSqlite(_connection).Options);
        _context.Database.EnsureCreated();

        _empresa = new Empresa("Sindicato Teste", "12.345.678/0001-90");
        _banco = new Conta(_empresa.Id, "Sicoob", TipoConta.Banco, 1000m);
        _caixa = new Conta(_empresa.Id, "Caixa", TipoConta.Caixa, 0m);
        var mensalidades = new Categoria(_empresa.Id, "Mensalidades", TipoLancamento.Entrada);
        _context.AddRange(_empresa, _banco, _caixa, mensalidades);
        _context.Lancamentos.Add(new Lancamento(_empresa.Id, _banco, mensalidades, TipoLancamento.Entrada, _hoje, 100m, FormaPagamento.Pix));
        _context.SaveChanges();
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
        if (Directory.Exists(_diretorioDestino))
            Directory.Delete(_diretorioDestino, recursive: true);
    }

    private LancamentoService CriarLancamentoService() => new(
        new LancamentoRepository(_context), new ContaRepository(_context), new CategoriaRepository(_context),
        new TransferenciaRepository(_context), _context);

    private Task RegistrarSaqueAsync() =>
        new TransferenciaService(new TransferenciaRepository(_context), new ContaRepository(_context), _context)
            .RegistrarAsync(_empresa.Id, new NovaTransferenciaDto(_banco.Id, _caixa.Id, _hoje, 300m, "Saque para despesas"));

    [Fact]
    public async Task Deve_mover_saldo_entre_contas_sem_contar_como_entrada_ou_saida()
    {
        await RegistrarSaqueAsync();

        var extrato = await CriarLancamentoService().ObterExtratoConsolidadoAsync(_empresa.Id);
        Assert.Equal(1100m, extrato.SaldoTotal); // a transferência não cria nem some com dinheiro

        var linhasTransferencia = extrato.Lancamentos.Where(l => l.EhTransferencia).ToList();
        Assert.Equal(2, linhasTransferencia.Count);
        var saida = Assert.Single(linhasTransferencia, l => l.Conta == "Sicoob");
        Assert.Equal("Transferência para Caixa — Saque para despesas", saida.Descricao);
        Assert.Equal(800m, saida.SaldoAposLancamento); // 1000 + 100 (mensalidade) - 300
        Assert.Equal(300m, Assert.Single(linhasTransferencia, l => l.Conta == "Caixa").SaldoAposLancamento);

        var relatorio = await new RelatorioService(CriarLancamentoService(), new ContaRepository(_context), new LancamentoRepository(_context))
            .GerarRelatorioPeriodoAsync(_empresa.Id, _hoje.Year, _hoje.Month);
        Assert.Equal(100m, relatorio.TotalEntradas);
        Assert.Equal(0m, relatorio.TotalSaidas);
        Assert.DoesNotContain(relatorio.EntradasPorCategoria, c => c.Categoria == LancamentoService.NomeCategoriaTransferencia);
        Assert.Equal(3, relatorio.Extrato.Count);

        var contaService = new ContaService(new ContaRepository(_context), new LancamentoRepository(_context), new TransferenciaRepository(_context), _context);
        var contas = await contaService.ListarComSaldoAsync(_empresa.Id);
        Assert.Equal(800m, contas.Single(c => c.Nome == "Sicoob").SaldoAtual);
        Assert.Equal(300m, contas.Single(c => c.Nome == "Caixa").SaldoAtual);
        await Assert.ThrowsAsync<DomainException>(() => contaService.ExcluirContaAsync(_caixa.Id));
    }

    [Fact]
    public async Task Transferencia_excluida_deve_sumir_do_extrato_e_do_saldo()
    {
        await RegistrarSaqueAsync();
        var transferencia = _context.Transferencias.Single();

        await new TransferenciaService(new TransferenciaRepository(_context), new ContaRepository(_context), _context)
            .ExcluirAsync(transferencia.Id);

        var extrato = await CriarLancamentoService().ObterExtratoConsolidadoAsync(_empresa.Id);
        Assert.DoesNotContain(extrato.Lancamentos, l => l.EhTransferencia);
        Assert.Equal(1100m, extrato.SaldoTotal);
    }

    [Fact]
    public async Task Exportacao_deve_levar_transferencias_em_arquivo_proprio()
    {
        await RegistrarSaqueAsync();

        var exportacaoService = new ExportacaoService(
            new EmpresaRepository(_context), new ContaRepository(_context), new CategoriaRepository(_context),
            new LancamentoRepository(_context), new ArmazenamentoAnexoService(), new ExportadorArquivoService(),
            new RemessaExportacaoRepository(_context), new TransferenciaRepository(_context), _context);

        var caminho = await exportacaoService.ExportarAsync(_empresa.Id, _hoje.Year, _hoje.Month, _diretorioDestino);

        using var zip = ZipFile.OpenRead(caminho);
        var transferenciasJson = Ler(zip, "transferencias.json");
        Assert.Contains("\"conta_origem\": \"Sicoob\"", transferenciasJson);
        Assert.Contains("\"conta_destino\": \"Caixa\"", transferenciasJson);
        Assert.DoesNotContain("Saque", Ler(zip, "lancamentos.json"));
        Assert.Contains("\"1.2\"", Ler(zip, "manifest.json"));
    }

    private static string Ler(ZipArchive zip, string nome)
    {
        using var leitor = new StreamReader(zip.GetEntry(nome)!.Open());
        return leitor.ReadToEnd();
    }
}
