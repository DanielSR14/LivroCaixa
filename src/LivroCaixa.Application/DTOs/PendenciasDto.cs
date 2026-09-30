namespace LivroCaixa.Application.DTOs;

/// <param name="LancamentosSemComprovante">Lançamentos de competências ainda não exportadas, sem nenhum anexo.</param>
/// <param name="CompetenciasNaoExportadas">Meses já encerrados que têm lançamentos e ainda não foram exportados, do mais antigo ao mais recente.</param>
public record PendenciasDto(int LancamentosSemComprovante, IReadOnlyList<(int Ano, int Mes)> CompetenciasNaoExportadas)
{
    public bool TemPendencias => LancamentosSemComprovante > 0 || CompetenciasNaoExportadas.Count > 0;
}
