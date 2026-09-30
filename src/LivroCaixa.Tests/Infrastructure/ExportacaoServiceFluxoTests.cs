using System.IO.Compression;
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

public class ExportacaoServiceFluxoTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly LivroCaixaDbContext _context;
    private readonly string _diretorioDestino;
    private readonly string _arquivoAnexoTemporario;

    public ExportacaoServiceFluxoTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<LivroCaixaDbContext>().UseSqlite(_connection).Options;
        _context = new LivroCaixaDbContext(options);
        _context.Database.EnsureCreated();

        _diretorioDestino = Path.Combine(Path.GetTempPath(), $"contabil-export-teste-{Guid.NewGuid()}");
        Directory.CreateDirectory(_diretorioDestino);

        _arquivoAnexoTemporario = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.pdf");
        File.WriteAllText(_arquivoAnexoTemporario, "comprovante de teste");
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();

        if (Directory.Exists(_diretorioDestino))
            Directory.Delete(_diretorioDestino, recursive: true);

        if (File.Exists(_arquivoAnexoTemporario))
            File.Delete(_arquivoAnexoTemporario);
    }

    [Fact]
    public async Task Deve_gerar_arquivo_contabil_com_manifesto_lancamentos_e_anexo()
    {
        var empresa = new Empresa("Padaria Pão Quente", "12.345.678/0001-90", "Comércio");
        var conta = new Conta(empresa.Id, "Caixa", TipoConta.Caixa);
        var categoria = new Categoria(empresa.Id, "Serviços", TipoLancamento.Entrada);
        var dataLancamento = new DateOnly(DateTime.Today.Year, DateTime.Today.Month, 1);

        var lancamento = new Lancamento(
            empresa.Id, conta, categoria, TipoLancamento.Entrada, dataLancamento, 1250m,
            FormaPagamento.Pix, "Pagamento referente ao serviço X");

        _context.Empresas.Add(empresa);
        _context.Contas.Add(conta);
        _context.Categorias.Add(categoria);
        _context.Lancamentos.Add(lancamento);
        _context.SaveChanges();

        var armazenamento = new ArmazenamentoAnexoService();
        var lancamentoRepository = new LancamentoRepository(_context);
        var anexoService = new AnexoService(lancamentoRepository, armazenamento, _context);
        var anexo = await anexoService.AdicionarAsync(lancamento.Id, _arquivoAnexoTemporario);

        try
        {
            var exportacaoService = new ExportacaoService(
                new EmpresaRepository(_context),
                new ContaRepository(_context),
                new CategoriaRepository(_context),
                lancamentoRepository,
                armazenamento,
                new ExportadorArquivoService(),
                new RemessaExportacaoRepository(_context),
                new TransferenciaRepository(_context),
                _context);

            var caminhoGerado = await exportacaoService.ExportarAsync(
                empresa.Id, dataLancamento.Year, dataLancamento.Month, _diretorioDestino);

            Assert.True(File.Exists(caminhoGerado));
            Assert.EndsWith(".contabil", caminhoGerado);

            using var arquivoZip = ZipFile.OpenRead(caminhoGerado);
            var nomesEntradas = arquivoZip.Entries.Select(e => e.FullName).ToList();

            Assert.Contains("manifest.json", nomesEntradas);
            Assert.Contains("empresa.json", nomesEntradas);
            Assert.Contains("lancamentos.json", nomesEntradas);
            Assert.Contains("lancamentos.csv", nomesEntradas);
            Assert.Contains("anexos/000001.pdf", nomesEntradas);

            var manifestoJson = LerEntrada(arquivoZip, "manifest.json");
            Assert.Contains("\"records\": 1", manifestoJson);

            var lancamentosJson = LerEntrada(arquivoZip, "lancamentos.json");
            Assert.Contains("1250", lancamentosJson);
            Assert.Contains("Serviços", lancamentosJson);
            Assert.Contains("Caixa", lancamentosJson);
        }
        finally
        {
            var pastaAnexo = Path.Combine(LivroCaixa.Infrastructure.CaminhosAplicacao.DiretorioAnexos, lancamento.Id.ToString());
            if (Directory.Exists(pastaAnexo))
                Directory.Delete(pastaAnexo, recursive: true);
        }
    }

    [Fact]
    public async Task Deve_exportar_lancamentos_de_multiplas_contas_no_mesmo_periodo()
    {
        var empresa = new Empresa("Padaria Pão Quente", "12.345.678/0001-90");
        var caixa = new Conta(empresa.Id, "Caixa", TipoConta.Caixa);
        var banco = new Conta(empresa.Id, "Banco", TipoConta.Banco);
        var categoria = new Categoria(empresa.Id, "Serviços", TipoLancamento.Entrada);
        var dataLancamento = new DateOnly(DateTime.Today.Year, DateTime.Today.Month, 1);

        var lancamentoCaixa = new Lancamento(
            empresa.Id, caixa, categoria, TipoLancamento.Entrada, dataLancamento, 100m, FormaPagamento.Dinheiro);
        var lancamentoBanco = new Lancamento(
            empresa.Id, banco, categoria, TipoLancamento.Entrada, dataLancamento, 900m, FormaPagamento.Pix);

        _context.Empresas.Add(empresa);
        _context.Contas.AddRange(caixa, banco);
        _context.Categorias.Add(categoria);
        _context.Lancamentos.AddRange(lancamentoCaixa, lancamentoBanco);
        _context.SaveChanges();

        var exportacaoService = new ExportacaoService(
            new EmpresaRepository(_context),
            new ContaRepository(_context),
            new CategoriaRepository(_context),
            new LancamentoRepository(_context),
            new ArmazenamentoAnexoService(),
            new ExportadorArquivoService(),
            new RemessaExportacaoRepository(_context),
            new TransferenciaRepository(_context),
            _context);

        var caminhoGerado = await exportacaoService.ExportarAsync(
            empresa.Id, dataLancamento.Year, dataLancamento.Month, _diretorioDestino);

        using var arquivoZip = ZipFile.OpenRead(caminhoGerado);
        var manifestoJson = LerEntrada(arquivoZip, "manifest.json");
        Assert.Contains("\"records\": 2", manifestoJson);

        var lancamentosJson = LerEntrada(arquivoZip, "lancamentos.json");
        Assert.Contains("Caixa", lancamentosJson);
        Assert.Contains("Banco", lancamentosJson);
    }

    [Fact]
    public async Task Deve_rejeitar_exportacao_de_periodo_sem_lancamentos()
    {
        var empresa = new Empresa("Padaria Pão Quente", "12.345.678/0001-90");
        var conta = new Conta(empresa.Id, "Caixa", TipoConta.Caixa);
        _context.Empresas.Add(empresa);
        _context.Contas.Add(conta);
        _context.SaveChanges();

        var exportacaoService = new ExportacaoService(
            new EmpresaRepository(_context),
            new ContaRepository(_context),
            new CategoriaRepository(_context),
            new LancamentoRepository(_context),
            new ArmazenamentoAnexoService(),
            new ExportadorArquivoService(),
            new RemessaExportacaoRepository(_context),
            new TransferenciaRepository(_context),
            _context);

        await Assert.ThrowsAsync<DomainException>(() =>
            exportacaoService.ExportarAsync(empresa.Id, 2020, 1, _diretorioDestino));
    }

    private static string LerEntrada(ZipArchive arquivoZip, string nomeEntrada)
    {
        var entrada = arquivoZip.GetEntry(nomeEntrada)!;
        using var leitor = new StreamReader(entrada.Open());
        return leitor.ReadToEnd();
    }
}
