using LivroCaixa.Domain.Enums;

namespace LivroCaixa.Application.DTOs;

public record AtualizarContaDto(
    Guid ContaId,
    string Nome,
    TipoConta Tipo,
    decimal SaldoInicial,
    string? Agencia = null,
    string? NumeroConta = null,
    TipoContaBancaria? TipoContaBancaria = null);
