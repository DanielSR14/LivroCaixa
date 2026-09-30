using System.Text.Json.Serialization;

namespace LivroCaixa.Application.DTOs.Exportacao;

public record HistoricoExportacaoDto(
    [property: JsonPropertyName("lancamento_id")] string LancamentoId,
    [property: JsonPropertyName("tipo_evento")] string TipoEvento,
    [property: JsonPropertyName("data_hora")] DateTime DataHora,
    [property: JsonPropertyName("dados_anteriores")] string? DadosAnteriores);
