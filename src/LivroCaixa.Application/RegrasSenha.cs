using LivroCaixa.Domain.Exceptions;

namespace LivroCaixa.Application;

/// <summary>
/// Regra da senha em texto puro. Fica na Application, não no Domain, porque o Domain só vê
/// o hash (Usuario.SenhaHash) — a senha digitada nunca chega até lá.
/// </summary>
public static class RegrasSenha
{
    public const int TamanhoMinimo = 6;

    public static void Validar(string? senha)
    {
        if (string.IsNullOrWhiteSpace(senha) || senha.Length < TamanhoMinimo)
            throw new DomainException($"A senha precisa ter pelo menos {TamanhoMinimo} caracteres.");
    }
}
