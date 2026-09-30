using System.IO.Compression;
using System.Text.Json;
using LivroCaixa.Application.Services;
using LivroCaixa.Domain.Entities;
using LivroCaixa.Domain.Enums;
using LivroCaixa.Infrastructure.Database;
using LivroCaixa.Infrastructure.Export;
using LivroCaixa.Infrastructure.Files;
using LivroCaixa.Infrastructure.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace LivroCaixa.Tests.Infrastructure;

/// <summary>
/// Erros típicos de dinheiro e datas em banco: arredondamento de decimal (o SQLite não tem tipo
/// decimal — o EF grava como texto), limites de mês/ano bissexto/virada de ano, e registros
/// excluídos logicamente que continuam ocupando espaço em disco.
/// </summary>
public class BancoEDatasLimiteTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly LivroCaixaDbContext _context;
    private readonly string _diretorioDestino = Path.Combine(Path.GetTempPath(), $"contabil_limites_{Guid.NewGuid():N}");
    private readonly Empresa _empresa;
    private readonly Conta _conta;
    private readonly Categoria _entrada;

    public BancoEDatasLimiteTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        _context = NovoContexto();
        _context.Database.EnsureCreated();

        _empresa = new Empresa("Sindicato Teste", "12.345.678/0001-90");
        _conta = new Conta(_empresa.Id, "Caixa", TipoConta.Caixa, 0m);
        _entrada = new Categoria(_empresa.Id, "Mensalidades", TipoLancamento.Entrada);
        _context.AddRange(_empresa, _conta, _entrada);
        _context.SaveChanges();
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
        if (Directory.Exists(_diretorioDestino))
            Directory.Delete(_diretorioDestino, recursive: true);
    }

    private LivroCaixaDbContext NovoContexto() =>
        new(new DbContextOptionsBuilder<LivroCaixaDbContext>().UseSqlite(_connection).Options);

    private LancamentoService CriarLancamentoService(LivroCaixaDbContext contexto) => new(
        new LancamentoRepository(contexto), new ContaRepository(contexto), new CategoriaRepository(contexto),
        new TransferenciaRepository(contexto), contexto);

    private Lancamento Lancar(DateOnly data, decimal valor)
    {
        var lancamento = new Lancamento(_empresa.Id, _conta, _entrada, TipoLancamento.Entrada, data, valor, FormaPagamento.Pix);
        _context.Lancamentos.Add(lancamento);
        return lancamento;
    }

    [Theory]
    [InlineData(0.01)]
    [InlineData(0.10)]
    [InlineData(1234567.89)]
    [InlineData(99999999.99)]
    public void Valor_deve_voltar_do_banco_exatamente_igual(double valor)
    {
        var lancamento = Lancar(new DateOnly(2026, 1, 15), (decimal)valor);
        _context.SaveChanges();

        using var outroContexto = NovoContexto();
        Assert.Equal((decimal)valor, outroContexto.Lancamentos.Single(l => l.Id == lancamento.Id).Valor);
    }

    // Com double/float, somar 0,10 mil vezes não dá 100 exato — o saldo "some" centavos.
    [Fact]
    public async Task Mil_lancamentos_de_dez_centavos_devem_somar_exatamente_cem_reais()
    {
        for (var i = 0; i < 1000; i++)
            Lancar(new DateOnly(2026, 1, 1).AddDays(i % 28), 0.10m);
        _context.SaveChanges();

        using var outroContexto = NovoContexto();
        var extrato = await CriarLancamentoService(outroContexto).ObterExtratoConsolidadoAsync(_empresa.Id);

        Assert.Equal(100.00m, extrato.SaldoTotal);
        Assert.Equal(100.00m, extrato.Lancamentos.Max(l => l.SaldoAposLancamento));
    }

    [Fact]
    public async Task Relatorio_de_fevereiro_bissexto_deve_incluir_o_dia_29_e_nada_de_fora_do_mes()
    {
        Lancar(new DateOnly(2024, 1, 31), 1m);
        Lancar(new DateOnly(2024, 2, 1), 10m);
        Lancar(new DateOnly(2024, 2, 29), 100m);
        Lancar(new DateOnly(2024, 3, 1), 1000m);
        _context.SaveChanges();

        var relatorio = await new RelatorioService(CriarLancamentoService(_context), new ContaRepository(_context), new LancamentoRepository(_context))
            .GerarRelatorioPeriodoAsync(_empresa.Id, 2024, 2);

        Assert.Equal(110m, relatorio.TotalEntradas);
        Assert.Equal(2, relatorio.Extrato.Count);
    }

    [Fact]
    public async Task Exportacao_de_dezembro_nao_deve_levar_o_primeiro_dia_do_ano_seguinte()
    {
        Lancar(new DateOnly(2025, 12, 31), 10m);
        Lancar(new DateOnly(2026, 1, 1), 20m);
        _context.SaveChanges();

        var caminho = await new ExportacaoService(
                new EmpresaRepository(_context), new ContaRepository(_context), new CategoriaRepository(_context),
                new LancamentoRepository(_context), new ArmazenamentoAnexoService(), new ExportadorArquivoService(),
                new RemessaExportacaoRepository(_context), new TransferenciaRepository(_context), _context)
            .ExportarAsync(_empresa.Id, 2025, 12, _diretorioDestino);

        using var zip = ZipFile.OpenRead(caminho);
        using var leitor = new StreamReader(zip.GetEntry("lancamentos.json")!.Open());
        using var json = JsonDocument.Parse(leitor.ReadToEnd());
        Assert.Equal("2025-12-31", Assert.Single(json.RootElement.EnumerateArray()).GetProperty("data").GetString());
    }

    [Fact]
    public async Task Pendencias_na_virada_do_ano_devem_apontar_dezembro_e_nao_o_janeiro_em_andamento()
    {
        Lancar(new DateOnly(2025, 12, 31), 10m);
        Lancar(new DateOnly(2026, 1, 2), 10m);
        _context.SaveChanges();

        var pendencias = await new PendenciasService(new LancamentoRepository(_context), new RemessaExportacaoRepository(_context))
            .ObterAsync(_empresa.Id, new DateTime(2026, 1, 5));

        Assert.Equal((2025, 12), Assert.Single(pendencias.CompetenciasNaoExportadas));
    }

    [Fact]
    public async Task Lancamento_excluido_nao_deve_contar_como_comprovante_pendente()
    {
        var lancamento = Lancar(DateOnly.FromDateTime(DateTime.Today), 10m);
        _context.SaveChanges();
        lancamento.Excluir();
        _context.SaveChanges();

        var pendencias = await new PendenciasService(new LancamentoRepository(_context), new RemessaExportacaoRepository(_context))
            .ObterAsync(_empresa.Id, DateTime.Today);

        Assert.Equal(0, pendencias.LancamentosSemComprovante);
    }

    // O lançamento excluído (exclusão lógica) continua com o comprovante no disco. Se o Armazenamento
    // o ignorar, esse arquivo nunca aparece no espaço ocupado e nunca é apagado pela limpeza.
    [Fact]
    public async Task Armazenamento_deve_contar_e_limpar_comprovante_de_lancamento_excluido()
    {
        var data = DateOnly.FromDateTime(DateTime.Today);
        var lancamento = Lancar(data, 10m);
        _context.SaveChanges();
        var anexo = new Anexo(lancamento.Id, Path.Combine(Guid.NewGuid().ToString(), "inexistente.pdf"), "nota.pdf", TipoArquivoAnexo.Pdf, 2048);
        _context.Anexos.Add(anexo);
        lancamento.Excluir();
        _context.RemessasExportacao.Add(new RemessaExportacao(_empresa.Id, data.Year, data.Month, 1, @"C:\x.contabil"));
        _context.SaveChanges();

        var lancamentoRepository = new LancamentoRepository(_context);
        var armazenamento = new ArmazenamentoService(
            new ContaRepository(_context), lancamentoRepository, new RemessaExportacaoRepository(_context),
            new AnexoService(lancamentoRepository, new ArmazenamentoAnexoService(), _context));

        var resumo = Assert.Single(await armazenamento.ListarResumoPorCompetenciaAsync(_empresa.Id));
        Assert.Equal(1, resumo.QuantidadeAnexos);
        Assert.Equal(2048, resumo.TamanhoTotalBytes);
        Assert.Equal(0, resumo.QuantidadeLancamentos); // o lançamento em si não conta: foi excluído

        Assert.Equal(1, await armazenamento.LimparAnexosDeCompetenciaAsync(_empresa.Id, data.Year, data.Month));
        using var outroContexto = NovoContexto();
        Assert.Empty(outroContexto.Anexos);
    }
}
