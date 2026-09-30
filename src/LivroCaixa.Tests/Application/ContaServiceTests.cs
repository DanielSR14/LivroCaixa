using LivroCaixa.Application.DTOs;
using LivroCaixa.Application.Services;
using LivroCaixa.Domain.Entities;
using LivroCaixa.Domain.Enums;
using LivroCaixa.Domain.Exceptions;
using LivroCaixa.Tests.Application.Fakes;

namespace LivroCaixa.Tests.Application;

public class ContaServiceTests
{
    [Fact]
    public async Task Deve_criar_conta()
    {
        var contas = new FakeContaRepository();
        var servico = new ContaService(contas, new FakeLancamentoRepository(), new FakeTransferenciaRepository(), new FakeUnitOfWork());
        var empresaId = Guid.NewGuid();

        var conta = await servico.CriarContaAsync(empresaId, new NovaContaDto("Investimentos", TipoConta.Investimento, 5000m));

        Assert.Single(contas.Contas);
        Assert.Equal("Investimentos", conta.Nome);
        Assert.Equal(TipoConta.Investimento, conta.Tipo);
        Assert.Equal(5000m, conta.SaldoInicial);
    }

    [Fact]
    public async Task Deve_criar_conta_bancaria_com_agencia_conta_e_tipo()
    {
        var contas = new FakeContaRepository();
        var servico = new ContaService(contas, new FakeLancamentoRepository(), new FakeTransferenciaRepository(), new FakeUnitOfWork());
        var empresaId = Guid.NewGuid();

        var conta = await servico.CriarContaAsync(empresaId, new NovaContaDto(
            "Banco do Brasil", TipoConta.Banco, 0m, "1234-5", "98765-0", TipoContaBancaria.Corrente));

        Assert.Equal("1234-5", conta.Agencia);
        Assert.Equal("98765-0", conta.NumeroConta);
        Assert.Equal(TipoContaBancaria.Corrente, conta.TipoContaBancaria);
    }

    [Fact]
    public async Task Conta_nao_bancaria_deve_ficar_sem_dados_bancarios()
    {
        var contas = new FakeContaRepository();
        var servico = new ContaService(contas, new FakeLancamentoRepository(), new FakeTransferenciaRepository(), new FakeUnitOfWork());
        var empresaId = Guid.NewGuid();

        var conta = await servico.CriarContaAsync(empresaId, new NovaContaDto("Cofrinho", TipoConta.Caixa, 50m));

        Assert.Null(conta.Agencia);
        Assert.Null(conta.NumeroConta);
        Assert.Null(conta.TipoContaBancaria);
    }

    [Fact]
    public async Task Deve_listar_contas_com_saldo_atual_calculado()
    {
        var empresaId = Guid.NewGuid();
        var contas = new FakeContaRepository();
        var lancamentos = new FakeLancamentoRepository();
        var categorias = new FakeCategoriaRepository();

        var caixa = new Conta(empresaId, "Caixa", TipoConta.Caixa, 100m);
        var categoriaEntrada = new Categoria(empresaId, "Serviços", TipoLancamento.Entrada);
        contas.Contas.Add(caixa);
        categorias.Categorias.Add(categoriaEntrada);

        var lancamentoService = new LancamentoService(lancamentos, contas, categorias, new FakeTransferenciaRepository(), new FakeUnitOfWork());
        await lancamentoService.RegistrarAsync(empresaId, new NovoLancamentoDto(
            caixa.Id, categoriaEntrada.Id, TipoLancamento.Entrada,
            DateOnly.FromDateTime(DateTime.Today), 250m, FormaPagamento.Pix, null, null));

        var contaService = new ContaService(contas, lancamentos, new FakeTransferenciaRepository(), new FakeUnitOfWork());
        var resumo = await contaService.ListarComSaldoAsync(empresaId);

        Assert.Single(resumo);
        Assert.Equal("Caixa", resumo[0].Nome);
        Assert.Equal(100m, resumo[0].SaldoInicial);
        Assert.Equal(350m, resumo[0].SaldoAtual);
    }

    [Fact]
    public async Task Deve_atualizar_conta_sem_lancamentos()
    {
        var contas = new FakeContaRepository();
        var servico = new ContaService(contas, new FakeLancamentoRepository(), new FakeTransferenciaRepository(), new FakeUnitOfWork());
        var empresaId = Guid.NewGuid();
        var conta = await servico.CriarContaAsync(empresaId, new NovaContaDto("Cofrinho", TipoConta.Caixa, 50m));

        await servico.AtualizarContaAsync(new AtualizarContaDto(conta.Id, "Reserva", TipoConta.Caixa, 200m));

        Assert.Equal("Reserva", contas.Contas.Single().Nome);
        Assert.Equal(200m, contas.Contas.Single().SaldoInicial);
    }

    [Fact]
    public async Task Nao_deve_alterar_saldo_inicial_de_conta_com_lancamentos()
    {
        var contas = new FakeContaRepository();
        var lancamentos = new FakeLancamentoRepository();
        var categorias = new FakeCategoriaRepository();
        var empresaId = Guid.NewGuid();
        var contaService = new ContaService(contas, lancamentos, new FakeTransferenciaRepository(), new FakeUnitOfWork());
        var conta = await contaService.CriarContaAsync(empresaId, new NovaContaDto("Caixa", TipoConta.Caixa, 100m));
        var categoria = new Categoria(empresaId, "Serviços", TipoLancamento.Entrada);
        categorias.Categorias.Add(categoria);
        var lancamentoService = new LancamentoService(lancamentos, contas, categorias, new FakeTransferenciaRepository(), new FakeUnitOfWork());
        await lancamentoService.RegistrarAsync(empresaId, new NovoLancamentoDto(
            conta.Id, categoria.Id, TipoLancamento.Entrada,
            DateOnly.FromDateTime(DateTime.Today), 100m, FormaPagamento.Pix, null, null));

        await Assert.ThrowsAsync<DomainException>(() =>
            contaService.AtualizarContaAsync(new AtualizarContaDto(conta.Id, "Caixa", TipoConta.Caixa, 999m)));

        // Renomear (sem mudar o saldo) continua permitido mesmo com lançamentos.
        await contaService.AtualizarContaAsync(new AtualizarContaDto(conta.Id, "Caixa da loja", TipoConta.Caixa, 100m));
        Assert.Equal("Caixa da loja", contas.Contas.Single().Nome);
    }

    [Fact]
    public async Task Deve_excluir_conta_sem_lancamentos()
    {
        var contas = new FakeContaRepository();
        var servico = new ContaService(contas, new FakeLancamentoRepository(), new FakeTransferenciaRepository(), new FakeUnitOfWork());
        var empresaId = Guid.NewGuid();
        var conta = await servico.CriarContaAsync(empresaId, new NovaContaDto("Cofrinho", TipoConta.Caixa, 50m));

        await servico.ExcluirContaAsync(conta.Id);

        Assert.Empty(contas.Contas);
    }

    [Fact]
    public async Task Nao_deve_excluir_conta_com_lancamentos()
    {
        var contas = new FakeContaRepository();
        var lancamentos = new FakeLancamentoRepository();
        var categorias = new FakeCategoriaRepository();
        var empresaId = Guid.NewGuid();
        var contaService = new ContaService(contas, lancamentos, new FakeTransferenciaRepository(), new FakeUnitOfWork());
        var conta = await contaService.CriarContaAsync(empresaId, new NovaContaDto("Caixa", TipoConta.Caixa, 100m));
        var categoria = new Categoria(empresaId, "Serviços", TipoLancamento.Entrada);
        categorias.Categorias.Add(categoria);
        var lancamentoService = new LancamentoService(lancamentos, contas, categorias, new FakeTransferenciaRepository(), new FakeUnitOfWork());
        await lancamentoService.RegistrarAsync(empresaId, new NovoLancamentoDto(
            conta.Id, categoria.Id, TipoLancamento.Entrada,
            DateOnly.FromDateTime(DateTime.Today), 100m, FormaPagamento.Pix, null, null));

        await Assert.ThrowsAsync<DomainException>(() => contaService.ExcluirContaAsync(conta.Id));
        Assert.Single(contas.Contas);
    }
}
