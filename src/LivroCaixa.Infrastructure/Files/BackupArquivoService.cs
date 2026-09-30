using System.IO.Compression;
using LivroCaixa.Application.Interfaces;
using Microsoft.Data.Sqlite;

namespace LivroCaixa.Infrastructure.Files;

public class BackupArquivoService : IBackupArquivoService
{
    public Task<string> CriarBackupAsync(string pastaDestino) =>
        CriarBackupAsync(pastaDestino, CaminhosAplicacao.CaminhoBancoDados, CaminhosAplicacao.DiretorioAnexos);

    /// <summary>
    /// Overload com os caminhos de origem explícitos (não só os de produção via <see cref="CaminhosAplicacao"/>) —
    /// existe para permitir testar a mecânica real de backup (SQLite hot-backup + zip) contra um
    /// banco/pasta de anexos isolados, sem depender do banco de dados real do usuário.
    /// </summary>
    /// <remarks>
    /// Roda num thread do pool: a cópia do banco e a compactação de todos os comprovantes são
    /// síncronas e podem levar vários segundos — no thread da UI, a janela congelava durante o
    /// backup automático da abertura do app. Não toca no DbContext (usa conexão SQLite própria),
    /// então é seguro fora do thread da UI.
    /// </remarks>
    public Task<string> CriarBackupAsync(string pastaDestino, string caminhoBancoDados, string diretorioAnexos) =>
        Task.Run(() => CriarBackup(pastaDestino, caminhoBancoDados, diretorioAnexos));

    private static string CriarBackup(string pastaDestino, string caminhoBancoDados, string diretorioAnexos)
    {
        Directory.CreateDirectory(pastaDestino);

        var nomeArquivo = $"LivroCaixa_Backup_{DateTime.Now:yyyy-MM-dd_HHmmss}.zip";
        var caminhoCompleto = Path.Combine(pastaDestino, nomeArquivo);

        if (File.Exists(caminhoCompleto))
            File.Delete(caminhoCompleto);

        var caminhoBancoTemporario = Path.Combine(Path.GetTempPath(), $"contabil_backup_{Guid.NewGuid():N}.db");
        try
        {
            CopiarBancoDeDadosDeFormaSegura(caminhoBancoDados, caminhoBancoTemporario);

            using var arquivoZip = ZipFile.Open(caminhoCompleto, ZipArchiveMode.Create);
            arquivoZip.CreateEntryFromFile(caminhoBancoTemporario, "contabil.db");

            if (Directory.Exists(diretorioAnexos))
            {
                foreach (var arquivo in Directory.EnumerateFiles(diretorioAnexos, "*", SearchOption.AllDirectories))
                {
                    var caminhoRelativo = Path.GetRelativePath(diretorioAnexos, arquivo).Replace('\\', '/');
                    arquivoZip.CreateEntryFromFile(arquivo, $"Anexos/{caminhoRelativo}");
                }
            }
        }
        finally
        {
            if (File.Exists(caminhoBancoTemporario))
                File.Delete(caminhoBancoTemporario);
        }

        return caminhoCompleto;
    }

    /// <summary>
    /// Usa a API nativa de backup do SQLite (não um File.Copy) para garantir uma cópia
    /// consistente mesmo com o app rodando e o DbContext mantendo a conexão principal aberta.
    /// "Pooling=False" é necessário para o arquivo temporário poder ser apagado logo em seguida:
    /// com pooling (padrão do Microsoft.Data.Sqlite), o handle do arquivo continua aberto no
    /// pool de conexões mesmo depois do using/Dispose(), e o File.Delete falha com
    /// "arquivo em uso" — pego por BackupArquivoServiceFluxoTests, que fazia exatamente isso.
    /// </summary>
    private static void CopiarBancoDeDadosDeFormaSegura(string caminhoBancoDados, string caminhoDestino)
    {
        using var origem = new SqliteConnection($"Data Source={caminhoBancoDados};Mode=ReadOnly;Pooling=False");
        using var destino = new SqliteConnection($"Data Source={caminhoDestino};Pooling=False");
        origem.Open();
        destino.Open();
        origem.BackupDatabase(destino);
    }
}
