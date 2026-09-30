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

public class AnexoServiceFluxoTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly LivroCaixaDbContext _context;
    private readonly string _arquivoTemporario;
    private Guid _lancamentoId;

    public AnexoServiceFluxoTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<LivroCaixaDbContext>().UseSqlite(_connection).Options;
        _context = new LivroCaixaDbContext(options);
        _context.Database.EnsureCreated();

        _arquivoTemporario = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.pdf");
        File.WriteAllText(_arquivoTemporario, "conteúdo de teste");
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();

        if (File.Exists(_arquivoTemporario))
            File.Delete(_arquivoTemporario);

        if (_lancamentoId != Guid.Empty)
        {
            var pastaAnexos = Path.Combine(CaminhosAplicacao.DiretorioAnexos, _lancamentoId.ToString());
            if (Directory.Exists(pastaAnexos))
                Directory.Delete(pastaAnexos, recursive: true);
        }
    }

    private (AnexoService Servico, Lancamento Lancamento) CriarCenario()
    {
        var empresa = new Empresa("Padaria Pão Quente", "12.345.678/0001-90");
        var conta = new Conta(empresa.Id, "Caixa", TipoConta.Caixa);
        var categoria = new Categoria(empresa.Id, "Serviços", TipoLancamento.Entrada);
        var lancamento = new Lancamento(
            empresa.Id, conta, categoria, TipoLancamento.Entrada,
            DateOnly.FromDateTime(DateTime.Today), 100m, FormaPagamento.Pix);

        _context.Empresas.Add(empresa);
        _context.Contas.Add(conta);
        _context.Categorias.Add(categoria);
        _context.Lancamentos.Add(lancamento);
        _context.SaveChanges();

        _lancamentoId = lancamento.Id;

        var servico = new AnexoService(new LancamentoRepository(_context), new ArmazenamentoAnexoService(), _context);
        return (servico, lancamento);
    }

    [Fact]
    public async Task Deve_copiar_arquivo_e_vincular_ao_lancamento()
    {
        var (servico, lancamento) = CriarCenario();

        var anexo = await servico.AdicionarAsync(lancamento.Id, _arquivoTemporario);

        Assert.True(File.Exists(servico.ObterCaminhoCompleto(anexo.CaminhoArquivo)));

        var lista = await servico.ListarAsync(lancamento.Id);
        Assert.Single(lista);
        Assert.Equal(Path.GetFileName(_arquivoTemporario), lista[0].NomeArquivo);
    }

    [Fact]
    public async Task Deve_rejeitar_extensao_nao_suportada()
    {
        var (servico, lancamento) = CriarCenario();
        var arquivoInvalido = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.exe");
        File.WriteAllText(arquivoInvalido, "x");

        try
        {
            await Assert.ThrowsAsync<DomainException>(() => servico.AdicionarAsync(lancamento.Id, arquivoInvalido));
        }
        finally
        {
            File.Delete(arquivoInvalido);
        }
    }

    [Fact]
    public async Task Deve_remover_anexo_e_apagar_arquivo_fisico()
    {
        var (servico, lancamento) = CriarCenario();
        var anexo = await servico.AdicionarAsync(lancamento.Id, _arquivoTemporario);
        var caminhoCompleto = servico.ObterCaminhoCompleto(anexo.CaminhoArquivo);

        await servico.RemoverAsync(lancamento.Id, anexo.Id);

        Assert.False(File.Exists(caminhoCompleto));
        Assert.Empty(await servico.ListarAsync(lancamento.Id));
    }
}
