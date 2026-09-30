using LivroCaixa.Domain.Enums;

namespace LivroCaixa.Application.DTOs;

public record LancamentoDetalheDto(
    Guid Id,
    Guid ContaId,
    Guid CategoriaId,
    TipoLancamento Tipo,
    DateOnly Data,
    decimal Valor,
    FormaPagamento FormaPagamento,
    string? Descricao,
    string? Observacoes);
