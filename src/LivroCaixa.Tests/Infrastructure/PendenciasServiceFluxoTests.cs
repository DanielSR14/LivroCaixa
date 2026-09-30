using LivroCaixa.Application.Services;
using LivroCaixa.Domain.Entities;
using LivroCaixa.Domain.Enums;
using LivroCaixa.Infrastructure.Database;
using LivroCaixa.Infrastructure.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace LivroCaixa.Tests.Infrastructure;

public class PendenciasServiceFluxoTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly LivroCaixaDbContext _context;
    private readonly PendenciasService _service;
    private readonly Empresa _empresa;
    private readonly Conta _conta;
    private readonly Categoria _categoria;

    public PendenciasServiceFluxoTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        var options = new DbContextOptionsBuilder<LivroCaixaDbContext>().UseSqlite(_connection).Options;
        _context = new LivroCaixaDbContext(options);
        _context.Database.EnsureCreated();

        _service = new PendenciasService(new LancamentoRepository(_context), new RemessaExportacaoRepository(_context));

        _empresa = new Empresa("Sindicato Teste", "12.345.678/0001-90");
        _conta = new Conta(_empresa.Id, "Caixa", TipoConta.Caixa, 0m);
        _categoria = new Categoria(_empresa.Id, "Mensalidades", TipoLancamento.Entrada);
        _context.AddRange(_empresa, _conta, _categoria);
        _context.SaveChanges();
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    private Lancamento CriarLancamento(DateOnly data, bool comComprovante)
    {
        var lancamento = new Lancamento(_empresa.Id, _conta, _categoria, TipoLancamento.Entrada, data, 10m, FormaPagamento.Pix);
        _context.Lancamentos.Add(lancamento);
        if (comComprovante)
        {
            var anexo = new Anexo(lancamento.Id, "x/c.pdf", "c.pdf", TipoArquivoAnexo.Pdf, 100);
            lancamento.AdicionarAnexo(anexo);
            _context.Anexos.Add(anexo);
        }
        _context.SaveChanges();
        return lancamento;
    }

    [Fact]
    public async Task Deve_apontar_comprovantes_faltando_e_meses_fechados_nao_exportados()
    {
        // Julho já exportado: nem o lançamento sem comprovante nem o mês contam mais como pendência.
        CriarLancamento(new DateOnly(2026, 7, 10), comComprovante: false);
        _context.RemessasExportacao.Add(new RemessaExportacao(_empresa.Id, 2026, 7, 1, @"C:\x.contabil"));
        // Agosto: mês fechado, não exportado, um lançamento com e um sem comprovante.
        CriarLancamento(new DateOnly(2026, 8, 5), comComprovante: true);
        CriarLancamento(new DateOnly(2026, 8, 6), comComprovante: false);
        // Setembro: mês atual — conta o comprovante faltando, mas ainda não é pendência de exportação.
        CriarLancamento(new DateOnly(2026, 9, 1), comComprovante: false);
        _context.SaveChanges();

        var pendencias = await _service.ObterAsync(_empresa.Id, new DateTime(2026, 9, 15));

        Assert.Equal(2, pendencias.LancamentosSemComprovante);
        Assert.Equal((2026, 8), Assert.Single(pendencias.CompetenciasNaoExportadas));
        Assert.True(pendencias.TemPendencias);
    }

    [Fact]
    public async Task Sem_lancamentos_nao_ha_pendencias()
    {
        var pendencias = await _service.ObterAsync(_empresa.Id, new DateTime(2026, 9, 15));

        Assert.False(pendencias.TemPendencias);
    }
}
