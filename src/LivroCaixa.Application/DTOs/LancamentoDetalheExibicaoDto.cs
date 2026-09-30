using LivroCaixa.Domain.Enums;

namespace LivroCaixa.Application.DTOs;

public record LancamentoDetalheExibicaoDto(
    Guid Id,
    TipoLancamento Tipo,
    DateOnly Data,
    decimal Valor,
    string Categoria,
    string Conta,
    TipoConta TipoConta,
    FormaPagamento FormaPagamento,
    string? Descricao,
    string? Observacoes,
    DateTime CriadoEm,
    DateTime? AlteradoEm,
    IReadOnlyList<AnexoResumoDto> Anexos);
