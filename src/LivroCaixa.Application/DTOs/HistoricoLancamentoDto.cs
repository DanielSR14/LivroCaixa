using LivroCaixa.Domain.Enums;

namespace LivroCaixa.Application.DTOs;

/// <param name="DadosAnteriores">Retrato JSON cru de antes do evento (ver RetratoLancamento).</param>
/// <param name="Alteracoes">Só em edições: os campos que mudaram, com valor de antes e de depois já formatados.</param>
public record HistoricoLancamentoDto(
    TipoEventoHistorico TipoEvento,
    DateTime DataHora,
    string? DadosAnteriores,
    IReadOnlyList<AlteracaoCampoDto> Alteracoes);

public record AlteracaoCampoDto(string Campo, string Antes, string Depois);
