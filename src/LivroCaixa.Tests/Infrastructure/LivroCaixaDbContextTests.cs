using LivroCaixa.Domain.Entities;
using LivroCaixa.Domain.Enums;
using LivroCaixa.Domain.Exceptions;
using LivroCaixa.Domain.Rules;
using LivroCaixa.Infrastructure.Database;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace LivroCaixa.Tests.Infrastructure;

public class LivroCaixaDbContextTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly LivroCaixaDbContext _context;

    public LivroCaixaDbContextTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _context = CriarContexto();
        _context.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    private LivroCaixaDbContext CriarContexto()
    {
        var options = new DbContextOptionsBuilder<LivroCaixaDbContext>()
            .UseSqlite(_connection)
            .Options;

        return new LivroCaixaDbContext(options);
    }

    [Fact]
    public void Deve_persistir_e_recuperar_lancamento_com_anexo()
    {
        var empresa = new Empresa("Padaria Pão Quente", "12.345.678/0001-90");
        var conta = new Conta(empresa.Id, "Caixa", TipoConta.Caixa, 100m);
        var categoria = new Categoria(empresa.Id, "Serviços", TipoLancamento.Entrada);

        var lancamento = new Lancamento(
            empresa.Id,
            conta,
            categoria,
            TipoLancamento.Entrada,
            DateOnly.FromDateTime(DateTime.Today),
            1250m,
            FormaPagamento.Pix,
            "Pagamento referente ao serviço X");

        var anexo = new Anexo(lancamento.Id, "abc123.pdf", "nota.pdf", TipoArquivoAnexo.Pdf, 2048);
        lancamento.AdicionarAnexo(anexo);

        _context.Empresas.Add(empresa);
        _context.Contas.Add(conta);
        _context.Categorias.Add(categoria);
        _context.Lancamentos.Add(lancamento);
        _context.SaveChanges();

        using var novoContexto = CriarContexto();
        var lancamentoRecuperado = novoContexto.Lancamentos
            .Include(l => l.Anexos)
            .Single(l => l.Id == lancamento.Id);

        Assert.Equal(1250m, lancamentoRecuperado.Valor);
        Assert.Equal(TipoLancamento.Entrada, lancamentoRecuperado.Tipo);
        Assert.Equal(FormaPagamento.Pix, lancamentoRecuperado.FormaPagamento);
        Assert.Single(lancamentoRecuperado.Anexos);
        Assert.Equal("abc123.pdf", lancamentoRecuperado.Anexos.First().CaminhoArquivo);
        Assert.Equal("nota.pdf", lancamentoRecuperado.Anexos.First().NomeOriginal);
    }

    [Fact]
    public void Deve_calcular_saldo_a_partir_de_lancamentos_persistidos()
    {
        var empresa = new Empresa("Padaria Pão Quente", "12.345.678/0001-90");
        var conta = new Conta(empresa.Id, "Caixa", TipoConta.Caixa, 100m);
        var categoriaEntrada = new Categoria(empresa.Id, "Serviços", TipoLancamento.Entrada);
        var categoriaSaida = new Categoria(empresa.Id, "Fornecedores", TipoLancamento.Saida);
        var hoje = DateOnly.FromDateTime(DateTime.Today);

        _context.Empresas.Add(empresa);
        _context.Contas.Add(conta);
        _context.Categorias.AddRange(categoriaEntrada, categoriaSaida);
        _context.Lancamentos.AddRange(
            new Lancamento(empresa.Id, conta, categoriaEntrada, TipoLancamento.Entrada, hoje, 300m, FormaPagamento.Pix),
            new Lancamento(empresa.Id, conta, categoriaSaida, TipoLancamento.Saida, hoje, 80m, FormaPagamento.Dinheiro));
        _context.SaveChanges();

        using var novoContexto = CriarContexto();
        var lancamentosDaConta = novoContexto.Lancamentos.Where(l => l.ContaId == conta.Id).ToList();
        var saldo = CalculadoraSaldo.CalcularSaldoAtual(conta, lancamentosDaConta);

        Assert.Equal(320m, saldo);
    }

    [Fact]
    public void Deve_impedir_categoria_de_outra_empresa_ao_recarregar_regra_de_negocio()
    {
        var empresaA = new Empresa("Padaria Pão Quente", "12.345.678/0001-90");
        var empresaB = new Empresa("Mercado Bom Preço", "98.765.432/0001-10");
        var contaEmpresaA = new Conta(empresaA.Id, "Caixa", TipoConta.Caixa);
        var categoriaEmpresaB = new Categoria(empresaB.Id, "Vendas", TipoLancamento.Entrada);

        _context.Empresas.AddRange(empresaA, empresaB);
        _context.Contas.Add(contaEmpresaA);
        _context.Categorias.Add(categoriaEmpresaB);
        _context.SaveChanges();

        Assert.Throws<DomainException>(() => new Lancamento(
            empresaA.Id,
            contaEmpresaA,
            categoriaEmpresaB,
            TipoLancamento.Entrada,
            DateOnly.FromDateTime(DateTime.Today),
            100m,
            FormaPagamento.Dinheiro));
    }

    [Fact]
    public async Task Descartar_alteracoes_pendentes_deve_destravar_salvamentos_apos_uma_falha()
    {
        var empresa = new Empresa("Sindicato Teste", "12.345.678/0001-90");
        var conta = new Conta(empresa.Id, "Caixa", TipoConta.Caixa, 0m);
        _context.AddRange(empresa, conta);
        _context.SaveChanges();

        // Categoria nunca gravada no banco: o INSERT do lançamento viola a FK e falha.
        var categoriaInexistente = new Categoria(empresa.Id, "Fantasma", TipoLancamento.Entrada);
        _context.Lancamentos.Add(new Lancamento(
            empresa.Id, conta, categoriaInexistente, TipoLancamento.Entrada, new DateOnly(2026, 9, 1), 10m, FormaPagamento.Pix));
        await Assert.ThrowsAsync<DbUpdateException>(() => _context.SalvarAlteracoesAsync());

        // Sem descartar, a alteração que falhou continua pendente e derruba o próximo salvamento também.
        var categoriaValida = new Categoria(empresa.Id, "Mensalidades", TipoLancamento.Entrada);
        _context.Categorias.Add(categoriaValida);
        await Assert.ThrowsAsync<DbUpdateException>(() => _context.SalvarAlteracoesAsync());

        _context.DescartarAlteracoesPendentes();
        _context.Categorias.Add(new Categoria(empresa.Id, "Doações", TipoLancamento.Entrada));
        await _context.SalvarAlteracoesAsync();

        using var novoContexto = CriarContexto();
        Assert.Equal("Doações", Assert.Single(novoContexto.Categorias).Nome);
        Assert.Empty(novoContexto.Lancamentos);
    }
}
