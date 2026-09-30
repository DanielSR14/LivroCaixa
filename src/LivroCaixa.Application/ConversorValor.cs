using System.Globalization;
using System.Text.RegularExpressions;

namespace LivroCaixa.Application;

/// <summary>
/// Lê e exibe o valor em reais dos formulários, sempre no padrão brasileiro e independente da
/// cultura do Windows:
/// <list type="bullet">
/// <item>com vírgula, é o formato brasileiro ("1.500,00" = mil e quinhentos);</item>
/// <item>sem vírgula, pontos em grupos de 3 dígitos são milhar ("1.500" = mil e quinhentos, e não 1,5
/// — ler como decimal deixaria o lançamento 1000 vezes menor sem aviso);</item>
/// <item>qualquer outro ponto é decimal ("1500.50", "10.5"), para quem digita no formato americano.</item>
/// </list>
/// </summary>
public static class ConversorValor
{
    private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");
    private static readonly Regex SoMilharComPonto = new(@"^\d{1,3}(\.\d{3})+$", RegexOptions.Compiled);

    public static bool TentarLer(string? texto, out decimal valor)
    {
        valor = 0m;
        if (string.IsNullOrWhiteSpace(texto))
            return false;

        var limpo = texto.Replace("R$", string.Empty).Trim();

        if (limpo.Contains(',') || SoMilharComPonto.IsMatch(limpo))
            return decimal.TryParse(limpo, NumberStyles.Number, PtBr, out valor);

        return decimal.TryParse(limpo, NumberStyles.Number, CultureInfo.InvariantCulture, out valor);
    }

    /// <summary>Campo em branco é válido e vira null (ex: valor padrão opcional de um modelo).</summary>
    public static bool TentarLerOpcional(string? texto, out decimal? valor)
    {
        valor = null;
        if (string.IsNullOrWhiteSpace(texto))
            return true;

        if (!TentarLer(texto, out var lido))
            return false;

        valor = lido;
        return true;
    }

    /// <summary>
    /// Texto para preencher o campo ao editar ("1.500,00"). Sempre com vírgula decimal, para que
    /// <see cref="TentarLer"/> leia de volta exatamente o mesmo valor (até 4 casas decimais).
    /// </summary>
    public static string Formatar(decimal valor) => valor.ToString("#,##0.00##", PtBr);
}
