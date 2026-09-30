using System.Text.Json.Serialization;

namespace LivroCaixa.Application.DTOs.Exportacao;

public record LancamentoExportacaoDto(
    [property: JsonPropertyName("data")] string Data,
    [property: JsonPropertyName("tipo")] string Tipo,
    [property: JsonPropertyName("valor")] decimal Valor,
    [property: JsonPropertyName("categoria")] string Categoria,
    [property: JsonPropertyName("conta")] string Conta,
    [property: JsonPropertyName("forma_pagamento")] string FormaPagamento,
    [property: JsonPropertyName("descricao")] string? Descricao,
    [property: JsonPropertyName("observacoes")] string? Observacoes,
    [property: JsonPropertyName("anexos")] List<string> NomesAnexos);
