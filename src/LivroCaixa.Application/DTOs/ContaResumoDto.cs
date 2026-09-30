using LivroCaixa.Domain.Enums;

namespace LivroCaixa.Application.DTOs;

public record ContaResumoDto(
    Guid Id,
    string Nome,
    TipoConta Tipo,
    decimal SaldoInicial,
    decimal SaldoAtual,
    string? Agencia = null,
    string? NumeroConta = null,
    TipoContaBancaria? TipoContaBancaria = null)
{
    /// <summary>Ex: "Ag. 1234 / Cc 56789-0 (Corrente)", ou null quando não é uma conta bancária.</summary>
    public string? DadosBancariosExibicao
    {
        get
        {
            if (Agencia is null && NumeroConta is null)
                return null;

            var partes = new List<string>();
            if (Agencia is not null)
                partes.Add($"Ag. {Agencia}");
            if (NumeroConta is not null)
                partes.Add($"Conta {NumeroConta}");

            var texto = string.Join(" / ", partes);
            return TipoContaBancaria is null ? texto : $"{texto} ({TipoContaBancaria})";
        }
    }
}
