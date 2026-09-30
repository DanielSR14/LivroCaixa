using System.Text.Json.Serialization;

namespace LivroCaixa.Application.DTOs.Exportacao;

public record ManifestoExportacaoDto(
    [property: JsonPropertyName("version")] string Version,
    [property: JsonPropertyName("company_id")] string CompanyId,
    [property: JsonPropertyName("period")] string Period,
    [property: JsonPropertyName("generated_at")] DateTime GeneratedAt,
    [property: JsonPropertyName("records")] int Records);
