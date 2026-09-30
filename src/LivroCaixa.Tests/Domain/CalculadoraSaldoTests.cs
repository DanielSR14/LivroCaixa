using LivroCaixa.Domain.Entities;
using LivroCaixa.Domain.Enums;
using LivroCaixa.Domain.Rules;

namespace LivroCaixa.Tests.Domain;

public class CalculadoraSaldoTests
{
    [Fact]
    public void Deve_calcular_saldo_somando_entradas_e_subtraindo_saidas()
    {
        var empresaId = Guid.NewGuid();
        var conta = new Conta(empresaId, "Caixa", TipoConta.Caixa, saldoInicial: 100m);
        var outraConta = new Conta(empresaId, "Banco", TipoConta.Banco, saldoInicial: 500m);
        var categoriaEntrada = new Categoria(empresaId, "Serviços", TipoLancamento.Entrada);
        var categoriaSaida = new Categoria(empresaId, "Fornecedores", TipoLancamento.Saida);
        var hoje = DateOnly.FromDateTime(DateTime.Today);

        var lancamentos = new[]
        {
            new Lancamento(empresaId, conta, categoriaEntrada, TipoLancamento.Entrada, hoje, 300m, FormaPagamento.Pix),
            new Lancamento(empresaId, conta, categoriaSaida, TipoLancamento.Saida, hoje, 80m, FormaPagamento.Dinheiro),
            // lançamento de outra conta não deve entrar no cálculo
            new Lancamento(empresaId, outraConta, categoriaEntrada, TipoLancamento.Entrada, hoje, 1000m, FormaPagamento.Pix)
        };

        var saldo = CalculadoraSaldo.CalcularSaldoAtual(conta, lancamentos);

        Assert.Equal(320m, saldo); // 100 (inicial) + 300 (entrada) - 80 (saída)
    }

    [Fact]
    public void Transferencia_deve_sair_da_origem_e_entrar_no_destino()
    {
        var empresaId = Guid.NewGuid();
        var banco = new Conta(empresaId, "Banco", TipoConta.Banco, saldoInicial: 1000m);
        var caixa = new Conta(empresaId, "Caixa", TipoConta.Caixa, saldoInicial: 0m);
        var terceira = new Conta(empresaId, "Cofrinho", TipoConta.Investimento, saldoInicial: 50m);
        var saque = new Transferencia(empresaId, banco, caixa, DateOnly.FromDateTime(DateTime.Today), 200m, "Saque");

        Assert.Equal(800m, CalculadoraSaldo.CalcularSaldoAtual(banco, Array.Empty<Lancamento>(), new[] { saque }));
        Assert.Equal(200m, CalculadoraSaldo.CalcularSaldoAtual(caixa, Array.Empty<Lancamento>(), new[] { saque }));
        Assert.Equal(50m, CalculadoraSaldo.CalcularSaldoAtual(terceira, Array.Empty<Lancamento>(), new[] { saque }));
    }

    [Fact]
    public void Deve_retornar_saldo_inicial_quando_nao_ha_lancamentos()
    {
        var empresaId = Guid.NewGuid();
        var conta = new Conta(empresaId, "Caixa", TipoConta.Caixa, saldoInicial: 250m);

        var saldo = CalculadoraSaldo.CalcularSaldoAtual(conta, Array.Empty<Lancamento>());

        Assert.Equal(250m, saldo);
    }
}
