namespace LivroCaixa.Application.DTOs;

public record NovaTransferenciaDto(Guid ContaOrigemId, Guid ContaDestinoId, DateOnly Data, decimal Valor, string? Descricao);
