using System.Security.Cryptography;
using System.Text;

namespace LivroCaixa.Application;

/// <summary>
/// Código de recuperação de acesso no formato "XXXX-XXXX-XXXX-XXXX". O alfabeto exclui caracteres
/// que se confundem ao copiar de um papel (0/O, 1/I/L): 31 símbolos × 16 posições ≈ 79 bits,
/// inviável de adivinhar mesmo sem o bloqueio por tentativas.
/// </summary>
public static class CodigoRecuperacao
{
    private const string Alfabeto = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";
    private const int TamanhoSemSeparadores = 16;

    public static string Gerar()
    {
        var codigo = new StringBuilder(TamanhoSemSeparadores + 3);
        for (var i = 0; i < TamanhoSemSeparadores; i++)
        {
            if (i > 0 && i % 4 == 0)
                codigo.Append('-');
            codigo.Append(Alfabeto[RandomNumberGenerator.GetInt32(Alfabeto.Length)]);
        }

        return codigo.ToString();
    }

    /// <summary>
    /// Forma canônica usada para gerar/verificar o hash: tolera o usuário digitar em minúsculas,
    /// com espaços ou sem os hífens.
    /// </summary>
    public static string Normalizar(string codigoDigitado) =>
        new(codigoDigitado.Where(char.IsLetterOrDigit).Select(char.ToUpperInvariant).ToArray());
}
