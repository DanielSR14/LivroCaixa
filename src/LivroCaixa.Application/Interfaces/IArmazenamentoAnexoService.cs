namespace LivroCaixa.Application.Interfaces;

public interface IArmazenamentoAnexoService
{
    Task<(string CaminhoArmazenado, long TamanhoBytes)> SalvarAsync(Guid lancamentoId, string caminhoOrigem);

    void Remover(string caminhoArmazenado);

    string ObterCaminhoCompleto(string caminhoArmazenado);

    /// <summary>O arquivo físico ainda está no disco? (pode ter sido apagado pela limpeza de Armazenamento)</summary>
    bool Existe(string caminhoArmazenado);
}
