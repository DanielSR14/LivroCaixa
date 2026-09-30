using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using LivroCaixa.Application.DTOs;
using LivroCaixa.Domain.Entities;
using LivroCaixa.Domain.Enums;

namespace LivroCaixa.Application;

/// <summary>
/// "Retrato" de um lançamento guardado em HistoricoLancamento.DadosAnteriores (JSON) e a comparação
/// entre dois retratos, para o histórico mostrar só o que mudou ("Valor: R$ 100,00 → R$ 150,00").
/// O formato do JSON também vai para o escritório (historico.json): campos só podem ser acrescentados,
/// nunca renomeados. Retratos antigos não têm Conta/Categoria — esses campos são simplesmente ignorados
/// na comparação quando faltam de um dos lados.
/// </summary>
public static class RetratoLancamento
{
    private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

    /// <summary>Campos na ordem em que aparecem na tela (chave no JSON, nome exibido).</summary>
    private static readonly (string Chave, string Nome)[] Campos =
    {
        ("Data", "Data"),
        ("Valor", "Valor"),
        ("Categoria", "Categoria"),
        ("Conta", "Conta"),
        ("FormaPagamento", "Forma de pagamento"),
        ("Descricao", "Descrição"),
        ("Observacoes", "Observações")
    };

    public static string Serializar(Lancamento lancamento, string? nomeConta, string? nomeCategoria) =>
        JsonSerializer.Serialize(new
        {
            lancamento.Data,
            lancamento.Valor,
            lancamento.FormaPagamento,
            lancamento.Descricao,
            lancamento.Observacoes,
            Conta = nomeConta,
            Categoria = nomeCategoria
        });

    /// <summary>
    /// Campos do retrato já formatados para exibição (data dd/MM/aaaa, valor em reais...), ou null se
    /// o JSON estiver ilegível — um retrato estragado não pode derrubar a tela de detalhes.
    /// </summary>
    public static IReadOnlyDictionary<string, string>? Ler(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        try
        {
            using var documento = JsonDocument.Parse(json);
            if (documento.RootElement.ValueKind != JsonValueKind.Object)
                return null;

            var campos = new Dictionary<string, string>();
            foreach (var (chave, _) in Campos)
            {
                if (documento.RootElement.TryGetProperty(chave, out var valor) && Formatar(chave, valor) is string texto)
                    campos[chave] = texto;
            }

            return campos;
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException or FormatException)
        {
            return null;
        }
    }

    /// <summary>Campos presentes nos dois retratos cujo valor mudou, na ordem de exibição.</summary>
    public static IReadOnlyList<AlteracaoCampoDto> Comparar(IReadOnlyDictionary<string, string> antes, IReadOnlyDictionary<string, string> depois) =>
        Campos
            .Where(c => antes.TryGetValue(c.Chave, out var a) && depois.TryGetValue(c.Chave, out var d) && a != d)
            .Select(c => new AlteracaoCampoDto(c.Nome, antes[c.Chave], depois[c.Chave]))
            .ToList();

    private const string Vazio = "(vazio)";

    private static string? Formatar(string chave, JsonElement valor) => chave switch
    {
        _ when valor.ValueKind == JsonValueKind.Null => Vazio,
        "Data" => DateOnly.ParseExact(valor.GetString()!, "yyyy-MM-dd", CultureInfo.InvariantCulture).ToString("dd/MM/yyyy", PtBr),
        "Valor" => valor.GetDecimal().ToString("C2", PtBr),
        // Sem JsonStringEnumConverter o enum é gravado como número; aceita texto também, por garantia.
        "FormaPagamento" => valor.ValueKind == JsonValueKind.Number
            ? NomeExibicao(((FormaPagamento)valor.GetInt32()).ToString())
            : NomeExibicao(valor.GetString()!),
        _ => valor.ValueKind == JsonValueKind.String
            ? (string.IsNullOrWhiteSpace(valor.GetString()) ? Vazio : valor.GetString())
            : null
    };

    // Mesma regra do EnumParaTextoExibicaoConverter da tela: "TransferenciaBancaria" → "Transferencia Bancaria".
    private static string NomeExibicao(string nomeEnum) => Regex.Replace(nomeEnum, "(?<!^)([A-Z])", " $1");
}
