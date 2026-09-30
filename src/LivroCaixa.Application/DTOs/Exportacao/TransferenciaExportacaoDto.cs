using System.Text.Json.Serialization;

namespace LivroCaixa.Application.DTOs.Exportacao;

/// <summary>
/// Linha de transferencias.json (manifesto versão 1.2+). Fica fora de lancamentos.json de propósito:
/// transferência entre contas da própria organização não é receita nem despesa.
/// </summary>
public record TransferenciaExportacaoDto(
    [property: JsonPropertyName("data")] string Data,
    [property: JsonPropertyName("conta_origem")] string ContaOrigem,
    [property: JsonPropertyName("conta_destino")] string ContaDestino,
    [property: JsonPropertyName("valor")] decimal Valor,
    [property: JsonPropertyName("descricao")] string? Descricao);
