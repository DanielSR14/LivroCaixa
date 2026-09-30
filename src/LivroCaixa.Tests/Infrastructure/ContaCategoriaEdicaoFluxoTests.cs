using LivroCaixa.Application.DTOs;
using LivroCaixa.Application.Services;
using LivroCaixa.Domain.Entities;
using LivroCaixa.Domain.Enums;
using LivroCaixa.Domain.Exceptions;
using LivroCaixa.Infrastructure.Database;
using LivroCaixa.Infrastructure.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace LivroCaixa.Tests.Infrastructure;

/// <summary>
/// Edição/exclusão de Conta e Categoria contra SQLite real — cobre especificamente a
/// restrição de FK (DeleteBehavior.Restrict de Lancamento para Conta/Categoria), que só o
/// banco de verdade aplica (um fake em memória não pegaria isso).
/// </summary>
public class ContaCategoriaEdicaoFluxoTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly LivroCaixaDbContext _context;
    private readonly ContaRepository _contaRepository;
    private readonly CategoriaRepository _categoriaRepository;
    private readonly LancamentoRepository _lancamentoRepository;
    private readonly ContaService _contaService;
    private readonly CategoriaService _categoriaService;
    private readonly Empresa _empresa;

    public ContaCategoriaEdicaoFluxoTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<LivroCaixaDbContext>().UseSqlite(_connection).Options;
        _context = new LivroCaixaDbContext(options);
        _context.Database.EnsureCreated();

        _contaRepository = new ContaRepository(_context);
        _categoriaRepository = new CategoriaRepository(_context);
        _lancamentoRepository = new LancamentoRepository(_context);
        _contaService = new ContaService(_contaRepository, _lancamentoRepository, new TransferenciaRepository(_context), _context);
        _categoriaService = new CategoriaService(_categoriaRepository, _lancamentoRepository, _context);

        _empresa = new Empresa("Sindicato Teste", "12.345.678/0001-90");
        _context.Empresas.Add(_empresa);
        _context.SaveChanges();
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    private LivroCaixaDbContext NovoContexto() =>
        new(new DbContextOptionsBuilder<LivroCaixaDbContext>().UseSqlite(_connection).Options);

    [Fact]
    public async Task Deve_persistir_atualizacao_de_conta_em_novo_contexto()
    {
        var conta = await _contaService.CriarContaAsync(_empresa.Id, new NovaContaDto("Cofrinho", TipoConta.Caixa, 50m));

        await _contaService.AtualizarContaAsync(new AtualizarContaDto(conta.Id, "Reserva de emergência", TipoConta.Caixa, 200m));

        using var novoContexto = NovoContexto();
        var contaRecarregada = await novoContexto.Contas.SingleAsync(c => c.Id == conta.Id);
        Assert.Equal("Reserva de emergência", contaRecarregada.Nome);
        Assert.Equal(200m, contaRecarregada.SaldoInicial);
    }

    [Fact]
    public async Task Deve_excluir_conta_sem_lancamentos_do_banco()
    {
        var conta = await _contaService.CriarContaAsync(_empresa.Id, new NovaContaDto("Cofrinho", TipoConta.Caixa, 50m));

        await _contaService.ExcluirContaAsync(conta.Id);

        using var novoContexto = NovoContexto();
        Assert.False(await novoContexto.Contas.AnyAsync(c => c.Id == conta.Id));
    }

    [Fact]
    public async Task Nao_deve_excluir_conta_com_lancamentos_e_conta_deve_continuar_no_banco()
    {
        var conta = await _contaService.CriarContaAsync(_empresa.Id, new NovaContaDto("Caixa", TipoConta.Caixa, 100m));
        var categoria = await _categoriaService.CriarAsync(_empresa.Id, "Mensalidades", TipoLancamento.Entrada);
        var lancamento = new Lancamento(
            _empresa.Id, await _contaRepository.ObterPorIdAsync(conta.Id) ?? conta,
            await _categoriaRepository.ObterPorIdAsync(categoria.Id) ?? categoria,
            TipoLancamento.Entrada, DateOnly.FromDateTime(DateTime.Today), 100m, FormaPagamento.Pix);
        await _lancamentoRepository.AdicionarAsync(lancamento);
        await _context.SaveChangesAsync();

        await Assert.ThrowsAsync<DomainException>(() => _contaService.ExcluirContaAsync(conta.Id));

        using var novoContexto = NovoContexto();
        Assert.True(await novoContexto.Contas.AnyAsync(c => c.Id == conta.Id));
    }

    [Fact]
    public async Task Deve_persistir_atualizacao_de_categoria_em_novo_contexto()
    {
        var categoria = await _categoriaService.CriarAsync(_empresa.Id, "Assinaturas", TipoLancamento.Saida);

        await _categoriaService.AtualizarAsync(categoria.Id, "Assinaturas de software");

        using var novoContexto = NovoContexto();
        var categoriaRecarregada = await novoContexto.Categorias.SingleAsync(c => c.Id == categoria.Id);
        Assert.Equal("Assinaturas de software", categoriaRecarregada.Nome);
    }

    [Fact]
    public async Task Deve_excluir_categoria_sem_lancamentos_do_banco()
    {
        var categoria = await _categoriaService.CriarAsync(_empresa.Id, "Assinaturas", TipoLancamento.Saida);

        await _categoriaService.ExcluirAsync(categoria.Id);

        using var novoContexto = NovoContexto();
        Assert.False(await novoContexto.Categorias.AnyAsync(c => c.Id == categoria.Id));
    }

    [Fact]
    public async Task Nao_deve_excluir_categoria_com_lancamentos_e_categoria_deve_continuar_no_banco()
    {
        var conta = await _contaService.CriarContaAsync(_empresa.Id, new NovaContaDto("Caixa", TipoConta.Caixa, 100m));
        var categoria = await _categoriaService.CriarAsync(_empresa.Id, "Mensalidades", TipoLancamento.Entrada);
        var lancamento = new Lancamento(
            _empresa.Id, await _contaRepository.ObterPorIdAsync(conta.Id) ?? conta,
            await _categoriaRepository.ObterPorIdAsync(categoria.Id) ?? categoria,
            TipoLancamento.Entrada, DateOnly.FromDateTime(DateTime.Today), 100m, FormaPagamento.Pix);
        await _lancamentoRepository.AdicionarAsync(lancamento);
        await _context.SaveChangesAsync();

        await Assert.ThrowsAsync<DomainException>(() => _categoriaService.ExcluirAsync(categoria.Id));

        using var novoContexto = NovoContexto();
        Assert.True(await novoContexto.Categorias.AnyAsync(c => c.Id == categoria.Id));
    }
}
