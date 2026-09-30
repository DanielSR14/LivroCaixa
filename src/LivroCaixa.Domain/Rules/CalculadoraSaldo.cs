using LivroCaixa.Domain.Entities;
using LivroCaixa.Domain.Enums;

namespace LivroCaixa.Domain.Rules;

public static class CalculadoraSaldo
{
    /// <summary>
    /// Saldo inicial + entradas - saídas da conta, mais o efeito das transferências (que saem da
    /// origem e entram no destino). Itens de outras contas são ignorados.
    /// </summary>
    public static decimal CalcularSaldoAtual(
        Conta conta,
        IEnumerable<Lancamento> lancamentos,
        IEnumerable<Transferencia>? transferencias = null)
    {
        var movimentoLancamentos = lancamentos
            .Where(l => l.ContaId == conta.Id)
            .Sum(l => l.Tipo == TipoLancamento.Entrada ? l.Valor : -l.Valor);

        var movimentoTransferencias = (transferencias ?? Enumerable.Empty<Transferencia>())
            .Sum(t => t.EfeitoNoSaldo(conta.Id));

        return conta.SaldoInicial + movimentoLancamentos + movimentoTransferencias;
    }
}
