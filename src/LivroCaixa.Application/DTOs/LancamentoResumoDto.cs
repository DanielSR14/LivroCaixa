using LivroCaixa.Domain.Enums;

namespace LivroCaixa.Application.DTOs;

public record LancamentoResumoDto(
    Guid Id,
    DateOnly Data,
    string Descricao,
    string Categoria,
    string Conta,
    Guid ContaId,
    TipoConta TipoConta,
    TipoLancamento Tipo,
    decimal Valor,
    decimal SaldoAposLancamento,
    int QuantidadeAnexos,
    bool FoiEditado,
    bool Excluido,
    string? Observacoes,
    bool EhTransferencia = false)
{
    /// <summary>
    /// Entra nos totais de entradas/saídas, gráficos e resultado por categoria? Transferência entre
    /// contas não entra: mexe no saldo de cada conta, mas não é receita nem despesa da organização.
    /// </summary>
    public bool ContaNoResultado => !EhTransferencia;
}
