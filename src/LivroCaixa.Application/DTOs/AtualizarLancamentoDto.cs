using LivroCaixa.Domain.Enums;

namespace LivroCaixa.Application.DTOs;

public record AtualizarLancamentoDto(
    Guid LancamentoId,
    Guid ContaId,
    Guid CategoriaId,
    DateOnly Data,
    decimal Valor,
    FormaPagamento FormaPagamento,
    string? Descricao,
    string? Observacoes);
