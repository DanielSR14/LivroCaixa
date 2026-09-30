using LivroCaixa.Domain.Enums;
using LivroCaixa.Domain.Exceptions;

namespace LivroCaixa.Domain.Entities;

public class Conta : EntidadeBase
{
    public Guid EmpresaId { get; private set; }
    public string Nome { get; private set; } = null!;
    public TipoConta Tipo { get; private set; }
    public decimal SaldoInicial { get; private set; }

    /// <summary>Só fazem sentido para Tipo == Banco; ficam nulos para os demais tipos.</summary>
    public string? Agencia { get; private set; }
    public string? NumeroConta { get; private set; }
    public TipoContaBancaria? TipoContaBancaria { get; private set; }

    private Conta()
    {
    }

    public Conta(
        Guid empresaId,
        string nome,
        TipoConta tipo,
        decimal saldoInicial = 0m,
        string? agencia = null,
        string? numeroConta = null,
        TipoContaBancaria? tipoContaBancaria = null)
    {
        if (empresaId == Guid.Empty)
            throw new DomainException("A conta precisa estar vinculada a uma empresa.");

        if (string.IsNullOrWhiteSpace(nome))
            throw new DomainException("O nome da conta é obrigatório.");

        EmpresaId = empresaId;
        Nome = nome.Trim();
        Tipo = tipo;
        SaldoInicial = saldoInicial;
        Agencia = NormalizarTexto(agencia);
        NumeroConta = NormalizarTexto(numeroConta);
        TipoContaBancaria = tipoContaBancaria;
    }

    public void Atualizar(
        string nome,
        TipoConta tipo,
        decimal saldoInicial,
        string? agencia = null,
        string? numeroConta = null,
        TipoContaBancaria? tipoContaBancaria = null)
    {
        if (string.IsNullOrWhiteSpace(nome))
            throw new DomainException("O nome da conta é obrigatório.");

        Nome = nome.Trim();
        Tipo = tipo;
        SaldoInicial = saldoInicial;
        Agencia = NormalizarTexto(agencia);
        NumeroConta = NormalizarTexto(numeroConta);
        TipoContaBancaria = tipoContaBancaria;
    }

    private static string? NormalizarTexto(string? texto) =>
        string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();
}
