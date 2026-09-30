using System.Globalization;

namespace LivroCaixa.Application;

/// <summary>
/// Comparação usada na busca livre: ignora maiúsculas e acentos ("agua" acha "Água", "acao" acha
/// "Ação"), porque ninguém digita acento em campo de busca. Usa a cultura pt-BR fixa, e não a do
/// Windows: em turco, por exemplo, "i" maiúsculo é "İ" e "IPTU" deixaria de achar "iptu".
/// </summary>
public static class BuscaTexto
{
    private static readonly CompareInfo ComparacaoPtBr = CultureInfo.GetCultureInfo("pt-BR").CompareInfo;

    public static bool Contem(string? texto, string busca) =>
        texto is not null &&
        ComparacaoPtBr.IndexOf(texto, busca.Trim(), CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace) >= 0;
}
