using LivroCaixa.Application.DTOs.Exportacao;

namespace LivroCaixa.Application.Interfaces;

public interface IExportadorArquivoService
{
    /// <returns>Caminho completo do arquivo .contabil gerado.</returns>
    Task<string> GerarArquivoAsync(
        PacoteExportacaoDto pacote,
        IReadOnlyList<InstrucaoCopiaAnexoDto> anexos,
        string diretorioDestino);
}
