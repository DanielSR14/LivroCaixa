using System.Text;
using LivroCaixa.Application.Interfaces;

namespace LivroCaixa.Infrastructure.Logs;

/// <summary>
/// Log em arquivo texto simples, um por dia (<c>livrocaixa-aaaa-mm-dd.log</c>), legível no Bloco
/// de Notas para o cliente poder anexar num e-mail. Escrito à mão em vez de usar Serilog: o app
/// só precisa de "gravar erro em arquivo", e cada dependência a menos é uma a menos para
/// acompanhar CVE (ver SEGURANCA.md).
/// </summary>
public class RegistroLogArquivo : IRegistroLog
{
    public const int DiasDeRetencao = 30;

    /// <summary>Teto por arquivo: um erro em laço não pode encher o disco do cliente.</summary>
    public const long TamanhoMaximoArquivoBytes = 10 * 1024 * 1024;

    // Estático porque o App cria uma instância própria antes da DI existir (para registrar erros
    // de inicialização) e a DI cria outra — as duas escrevem no mesmo arquivo.
    private static readonly object Trava = new();

    public string DiretorioLogs { get; }

    public RegistroLogArquivo() : this(CaminhosAplicacao.DiretorioLogs)
    {
    }

    /// <summary>Com diretório explícito — existe para os testes usarem uma pasta temporária.</summary>
    public RegistroLogArquivo(string diretorioLogs)
    {
        DiretorioLogs = diretorioLogs;
    }

    public void RegistrarInformacao(string mensagem) => Escrever("INFO", mensagem);

    public void RegistrarErro(string contexto, Exception excecao) =>
        Escrever("ERRO", $"{contexto}{Environment.NewLine}{excecao}");

    /// <summary>Apaga logs com mais de <see cref="DiasDeRetencao"/> dias. Chamado uma vez na abertura do app.</summary>
    public void RemoverLogsAntigos(DateTime agora)
    {
        try
        {
            if (!Directory.Exists(DiretorioLogs))
                return;

            var limite = agora.AddDays(-DiasDeRetencao);
            foreach (var arquivo in Directory.EnumerateFiles(DiretorioLogs, "livrocaixa-*.log"))
            {
                if (File.GetLastWriteTime(arquivo) < limite)
                    File.Delete(arquivo);
            }
        }
        catch
        {
            // Melhor esforço: não conseguir limpar logs antigos não é motivo para nada falhar.
        }
    }

    private void Escrever(string nivel, string texto)
    {
        try
        {
            var agora = DateTime.Now;
            var caminho = Path.Combine(DiretorioLogs, $"livrocaixa-{agora:yyyy-MM-dd}.log");
            var linha = $"{agora:yyyy-MM-dd HH:mm:ss} [{nivel}] {texto}{Environment.NewLine}";

            lock (Trava)
            {
                Directory.CreateDirectory(DiretorioLogs);
                if (File.Exists(caminho) && new FileInfo(caminho).Length > TamanhoMaximoArquivoBytes)
                    return;

                File.AppendAllText(caminho, linha, Encoding.UTF8);
            }
        }
        catch
        {
            // Nunca propagar: o log é chamado justamente de dentro do tratamento de erros.
        }
    }
}
