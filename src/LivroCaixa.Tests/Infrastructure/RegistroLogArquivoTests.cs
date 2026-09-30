using LivroCaixa.Infrastructure.Logs;

namespace LivroCaixa.Tests.Infrastructure;

public class RegistroLogArquivoTests : IDisposable
{
    private readonly string _diretorio = Path.Combine(Path.GetTempPath(), $"contabil_logs_{Guid.NewGuid():N}");

    public void Dispose()
    {
        if (Directory.Exists(_diretorio))
            Directory.Delete(_diretorio, recursive: true);
    }

    private string LerLogDeHoje() =>
        File.ReadAllText(Path.Combine(_diretorio, $"livrocaixa-{DateTime.Now:yyyy-MM-dd}.log"));

    [Fact]
    public void Deve_gravar_erro_com_contexto_tipo_e_pilha_da_excecao()
    {
        var registro = new RegistroLogArquivo(_diretorio);

        Exception capturada;
        try
        {
            throw new InvalidOperationException("banco travado", new IOException("arquivo em uso"));
        }
        catch (Exception ex)
        {
            capturada = ex;
        }

        registro.RegistrarErro("Falha ao salvar lançamento", capturada);

        var conteudo = LerLogDeHoje();
        Assert.Contains("[ERRO] Falha ao salvar lançamento", conteudo);
        Assert.Contains("InvalidOperationException: banco travado", conteudo);
        Assert.Contains("IOException: arquivo em uso", conteudo);
        Assert.Contains(nameof(Deve_gravar_erro_com_contexto_tipo_e_pilha_da_excecao), conteudo);
    }

    [Fact]
    public void Deve_acumular_entradas_no_mesmo_arquivo_do_dia()
    {
        var registro = new RegistroLogArquivo(_diretorio);

        registro.RegistrarInformacao("App iniciado");
        new RegistroLogArquivo(_diretorio).RegistrarInformacao("Outra instância");

        var conteudo = LerLogDeHoje();
        Assert.Contains("[INFO] App iniciado", conteudo);
        Assert.Contains("[INFO] Outra instância", conteudo);
        Assert.Single(Directory.GetFiles(_diretorio));
    }

    [Fact]
    public void Nao_deve_lancar_excecao_quando_nao_consegue_gravar()
    {
        // Um arquivo comum no lugar da pasta de logs faz Directory.CreateDirectory falhar.
        Directory.CreateDirectory(_diretorio);
        var caminhoInvalido = Path.Combine(_diretorio, "nao_e_pasta.txt");
        File.WriteAllText(caminhoInvalido, "x");
        var registro = new RegistroLogArquivo(caminhoInvalido);

        var excecao = Record.Exception(() => registro.RegistrarErro("teste", new Exception("x")));

        Assert.Null(excecao);
    }

    [Fact]
    public void Deve_remover_somente_logs_mais_antigos_que_a_retencao()
    {
        Directory.CreateDirectory(_diretorio);
        var agora = new DateTime(2026, 9, 29, 12, 0, 0);
        var antigo = Path.Combine(_diretorio, "livrocaixa-2026-08-01.log");
        var recente = Path.Combine(_diretorio, "livrocaixa-2026-09-20.log");
        var outroArquivo = Path.Combine(_diretorio, "nao-e-log.txt");
        foreach (var arquivo in new[] { antigo, recente, outroArquivo })
            File.WriteAllText(arquivo, "x");
        File.SetLastWriteTime(antigo, agora.AddDays(-(RegistroLogArquivo.DiasDeRetencao + 1)));
        File.SetLastWriteTime(recente, agora.AddDays(-9));
        File.SetLastWriteTime(outroArquivo, agora.AddDays(-365));

        new RegistroLogArquivo(_diretorio).RemoverLogsAntigos(agora);

        Assert.False(File.Exists(antigo));
        Assert.True(File.Exists(recente));
        Assert.True(File.Exists(outroArquivo));
    }
}
