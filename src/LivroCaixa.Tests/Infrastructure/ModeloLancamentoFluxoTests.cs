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

public class ModeloLancamentoFluxoTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly LivroCaixaDbContext _context;
    private readonly Empresa _empresa;
    private readonly Conta _banco;
    private readonly Categoria _aluguel;

    public ModeloLancamentoFluxoTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        _context = NovoContexto();
        _context.Database.EnsureCreated();

        _empresa = new Empresa("Sindicato Teste", "12.345.678/0001-90");
        _banco = new Conta(_empresa.Id, "Sicoob", TipoConta.Banco, 0m);
        _aluguel = new Categoria(_empresa.Id, "Aluguel", TipoLancamento.Saida);
        _context.AddRange(_empresa, _banco, _aluguel);
        _context.SaveChanges();
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    private LivroCaixaDbContext NovoContexto() =>
        new(new DbContextOptionsBuilder<LivroCaixaDbContext>().UseSqlite(_connection).Options);

    private static ModeloLancamentoService CriarServico(LivroCaixaDbContext contexto) =>
        new(new ModeloLancamentoRepository(contexto), new CategoriaRepository(contexto), new ContaRepository(contexto), contexto);

    private SalvarModeloLancamentoDto Aluguel(string codigo = " alu ") =>
        new(codigo, "Aluguel da sede", _aluguel.Id, _banco.Id, FormaPagamento.Boleto, 1500m, "Aluguel mensal");

    [Fact]
    public async Task Deve_criar_modelo_com_codigo_normalizado_e_tipo_vindo_da_categoria()
    {
        await CriarServico(_context).CriarAsync(_empresa.Id, Aluguel());

        using var outroContexto = NovoContexto();
        var modelo = Assert.Single(await CriarServico(outroContexto).ListarAsync(_empresa.Id));
        Assert.Equal("ALU", modelo.Codigo);
        Assert.Equal(TipoLancamento.Saida, modelo.Tipo);
        Assert.Equal("Aluguel", modelo.NomeCategoria);
        Assert.Equal("Sicoob", modelo.NomeConta);
        Assert.Equal(1500m, modelo.Valor);
        Assert.Equal("ALU — Aluguel da sede", modelo.Exibicao);
    }

    [Fact]
    public async Task Nao_deve_permitir_codigo_repetido_nem_invalido()
    {
        var servico = CriarServico(_context);
        await servico.CriarAsync(_empresa.Id, Aluguel("ALU"));

        await Assert.ThrowsAsync<DomainException>(() => servico.CriarAsync(_empresa.Id, Aluguel("alu")));
        await Assert.ThrowsAsync<DomainException>(() => servico.CriarAsync(_empresa.Id, Aluguel("A B")));
        await Assert.ThrowsAsync<DomainException>(() => servico.CriarAsync(_empresa.Id, Aluguel("CODIGOLONGO1")));
    }

    [Fact]
    public async Task Editar_pode_manter_o_proprio_codigo()
    {
        var servico = CriarServico(_context);
        var modelo = await servico.CriarAsync(_empresa.Id, Aluguel("ALU"));

        await servico.AtualizarAsync(modelo.Id, Aluguel("ALU") with { Valor = null, ContaId = null });

        using var outroContexto = NovoContexto();
        var atualizado = Assert.Single(await CriarServico(outroContexto).ListarAsync(_empresa.Id));
        Assert.Null(atualizado.Valor);
        Assert.Null(atualizado.ContaId);
    }

    [Fact]
    public async Task Excluir_conta_mantem_o_modelo_sem_conta_e_excluir_categoria_apaga_o_modelo()
    {
        await CriarServico(_context).CriarAsync(_empresa.Id, Aluguel());

        _context.Contas.Remove(_banco);
        _context.SaveChanges();
        using (var contexto = NovoContexto())
            Assert.Null(Assert.Single(await CriarServico(contexto).ListarAsync(_empresa.Id)).ContaId);

        _context.Categorias.Remove(_aluguel);
        _context.SaveChanges();
        using (var contexto = NovoContexto())
            Assert.Empty(await CriarServico(contexto).ListarAsync(_empresa.Id));
    }
}
