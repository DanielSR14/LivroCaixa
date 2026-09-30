namespace LivroCaixa.Application.Interfaces;

public interface IBackupArquivoService
{
    /// <returns>Caminho completo do arquivo .zip de backup gerado.</returns>
    Task<string> CriarBackupAsync(string pastaDestino);
}
