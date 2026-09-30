namespace LivroCaixa.Application.DTOs;

/// <summary>
/// Extrato consolidado + saldo total de todas as contas, calculados na mesma passada
/// (o Dashboard precisa dos dois e antes carregava todos os lançamentos duas vezes).
/// </summary>
public record ExtratoConsolidadoDto(IReadOnlyList<LancamentoResumoDto> Lancamentos, decimal SaldoTotal);
