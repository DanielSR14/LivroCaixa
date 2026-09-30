using LivroCaixa.Application.Services;
using LivroCaixa.Domain.Entities;
using LivroCaixa.Domain.Enums;
using LivroCaixa.Domain.Exceptions;
using LivroCaixa.Infrastructure;
using LivroCaixa.Infrastructure.Database;
using LivroCaixa.Infrastructure.Files;
using LivroCaixa.Infrastructure.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace LivroCaixa.Tests.Infrastructure;

public class ArmazenamentoServiceFluxoTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly LivroCaixaDbContext _context;
    private readonly List<string> _arquivosTemporarios = new();
    private readonly List<Guid> _lancamentosCriados = new();

    private readonly ContaRepository _contaRepository;
    private readonly LancamentoRepository _lancamentoRepository;
    private readonly RemessaExportacaoRepository _remessaRepository;
    private readonly AnexoService _anexoService;
    private readonly ArmazenamentoService _armazenamentoService;

    private readonly Empresa _empresa;
    private readonly Conta _conta;
    private readonly Categoria _categoria;

    public ArmazenamentoServiceFluxoTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<LivroCaixaDbContext>().UseSqlite(_connection).Options;
        _context = new LivroCaixaDbContext(options);
        _context.Database.EnsureCreated();

        _contaRepository = new ContaRepository(_context);
        _lancamentoRepository = new LancamentoRepository(_context);
        _remessaRepository = new RemessaExportacaoRepository(_context);
        _anexoService = new AnexoService(_lancamentoRepository, new ArmazenamentoAnexoService(), _context);
        _armazenamentoService = new ArmazenamentoService(_contaRepository, _lancamentoRepository, _remessaRepository, _anexoService);

        _empresa = new Empresa("Padaria Pão Quente", "12.345.678/0001-90");
        _conta = new Conta(_empresa.Id, "Caixa", TipoConta.Caixa);
        _categoria = new Categoria(_empresa.Id, "Serviços", TipoLancamento.Entrada);

        _context.Empresas.Add(_empresa);
        _context.Contas.Add(_conta);
        _context.Categorias.Add(_categoria);
        _context.SaveChanges();
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();

        foreach (var arquivo in _arquivosTemporarios.Where(File.Exists))
            File.Delete(arquivo);

        foreach (var lancamentoId in _lancamentosCriados)
        {
            var pastaAnexos = Path.Combine(CaminhosAplicacao.DiretorioAnexos, lancamentoId.ToString());
            if (Directory.Exists(pastaAnexos))
                Directory.Delete(pastaAnexos, recursive: true);
        }
    }

    private async Task<Lancamento> CriarLancamentoComAnexoAsync(DateOnly data, decimal valor)
    {
        var lancamento = new Lancamento(_empresa.Id, _conta, _categoria, TipoLancamento.Entrada, data, valor, FormaPagamento.Pix);
        _context.Lancamentos.Add(lancamento);
        _context.SaveChanges();
        _lancamentosCriados.Add(lancamento.Id);

        var arquivoTemporario = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.pdf");
        File.WriteAllText(arquivoTemporario, "comprovante de teste");
        _arquivosTemporarios.Add(arquivoTemporario);

        await _anexoService.AdicionarAsync(lancamento.Id, arquivoTemporario);

        return lancamento;
    }

    [Fact]
    public async Task Deve_agrupar_resumo_por_competencia_com_tamanho_e_status_de_exportacao()
    {
        var competenciaExportada = new DateOnly(2026, 7, 10);
        var competenciaNaoExportada = new DateOnly(2026, 8, 5);

        await CriarLancamentoComAnexoAsync(competenciaExportada, 100m);
        await CriarLancamentoComAnexoAsync(competenciaExportada, 200m);
        await CriarLancamentoComAnexoAsync(competenciaNaoExportada, 50m);

        await _remessaRepository.AdicionarAsync(new RemessaExportacao(_empresa.Id, 2026, 7, 2, "C:\\fake\\julho.contabil"));
        await _context.SalvarAlteracoesAsync();

        var resumo = await _armazenamentoService.ListarResumoPorCompetenciaAsync(_empresa.Id);

        Assert.Equal(2, resumo.Count);

        var julho = resumo.Single(r => r.Ano == 2026 && r.Mes == 7);
        Assert.Equal(2, julho.QuantidadeLancamentos);
        Assert.Equal(2, julho.QuantidadeAnexos);
        Assert.True(julho.TamanhoTotalBytes > 0);
        Assert.True(julho.JaExportada);

        var agosto = resumo.Single(r => r.Ano == 2026 && r.Mes == 8);
        Assert.Equal(1, agosto.QuantidadeLancamentos);
        Assert.False(agosto.JaExportada);
    }

    [Fact]
    public async Task Deve_rejeitar_limpeza_de_competencia_ainda_nao_exportada()
    {
        await CriarLancamentoComAnexoAsync(new DateOnly(2026, 8, 5), 50m);

        await Assert.ThrowsAsync<DomainException>(() =>
            _armazenamentoService.LimparAnexosDeCompetenciaAsync(_empresa.Id, 2026, 8));
    }

    [Fact]
    public async Task Deve_apagar_arquivos_fisicos_de_competencia_ja_exportada_sem_apagar_lancamento()
    {
        var lancamento1 = await CriarLancamentoComAnexoAsync(new DateOnly(2026, 7, 10), 100m);
        var lancamento2 = await CriarLancamentoComAnexoAsync(new DateOnly(2026, 7, 15), 200m);

        var anexo1 = (await _lancamentoRepository.ObterPorIdAsync(lancamento1.Id))!.Anexos.Single();
        var caminhoAnexo1 = _anexoService.ObterCaminhoCompleto(anexo1.CaminhoArquivo);

        await _remessaRepository.AdicionarAsync(new RemessaExportacao(_empresa.Id, 2026, 7, 2, "C:\\fake\\julho.contabil"));
        await _context.SalvarAlteracoesAsync();

        var quantidadeRemovida = await _armazenamentoService.LimparAnexosDeCompetenciaAsync(_empresa.Id, 2026, 7);

        Assert.Equal(2, quantidadeRemovida);
        Assert.False(File.Exists(caminhoAnexo1));

        var lancamento1Recarregado = await _lancamentoRepository.ObterPorIdAsync(lancamento1.Id);
        var lancamento2Recarregado = await _lancamentoRepository.ObterPorIdAsync(lancamento2.Id);

        Assert.NotNull(lancamento1Recarregado);
        Assert.NotNull(lancamento2Recarregado);
        Assert.Empty(lancamento1Recarregado!.Anexos);
        Assert.Empty(lancamento2Recarregado!.Anexos);
    }
}
