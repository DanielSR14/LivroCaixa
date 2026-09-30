namespace LivroCaixa.Application.DTOs.Exportacao;

/// <summary>
/// Dados que efetivamente viram conteúdo do arquivo .contabil (manifest.json, empresa.json,
/// lancamentos.json/csv). Não inclui os caminhos físicos dos anexos — isso é tratado à parte
/// por <see cref="InstrucaoCopiaAnexoDto"/>, resolvido pela Infrastructure.
/// </summary>
public record PacoteExportacaoDto(
    string Cnpj,
    int Ano,
    int Mes,
    ManifestoExportacaoDto Manifesto,
    EmpresaExportacaoDto Empresa,
    IReadOnlyList<LancamentoExportacaoDto> Lancamentos,
    IReadOnlyList<HistoricoExportacaoDto> Historico,
    IReadOnlyList<TransferenciaExportacaoDto> Transferencias);
