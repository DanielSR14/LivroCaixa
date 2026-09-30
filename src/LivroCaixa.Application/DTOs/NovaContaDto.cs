using LivroCaixa.Domain.Enums;

namespace LivroCaixa.Application.DTOs;

public record NovaContaDto(
    string Nome,
    TipoConta Tipo,
    decimal SaldoInicial,
    string? Agencia = null,
    string? NumeroConta = null,
    TipoContaBancaria? TipoContaBancaria = null);
