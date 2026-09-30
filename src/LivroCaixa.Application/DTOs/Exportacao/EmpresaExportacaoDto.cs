using System.Text.Json.Serialization;

namespace LivroCaixa.Application.DTOs.Exportacao;

public record EmpresaExportacaoDto(
    [property: JsonPropertyName("razao_social")] string RazaoSocial,
    [property: JsonPropertyName("nome_fantasia")] string? NomeFantasia,
    [property: JsonPropertyName("cnpj")] string Cnpj,
    [property: JsonPropertyName("segmento")] string? Segmento);
