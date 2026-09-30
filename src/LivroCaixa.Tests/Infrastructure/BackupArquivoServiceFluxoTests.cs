using System.IO.Compression;
using LivroCaixa.Domain.Entities;
using LivroCaixa.Domain.Enums;
using LivroCaixa.Infrastructure.Database;
using LivroCaixa.Infrastructure.Files;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace LivroCaixa.Tests.Infrastructure;

/// <summary>
/// Testa a mecânica real de backup (SQLite hot-backup via <see cref="SqliteConnection.BackupDatabase"/> + zip)
/// contra um banco de dados e uma pasta de anexos isolados em disco (nunca o banco real do usuário —
/// ver o overload de <see cref="BackupArquivoService.CriarBackupAsync(string, string, string)"/>).
/// </summary>
public class BackupArquivoServiceFluxoTests : IDisposable
{
    private readonly string _pastaTemporaria;
    private readonly string _caminhoBancoOrigem;
    private readonly string _diretorioAnexos;
    private readonly string _pastaDestino;

    public BackupArquivoServiceFluxoTests()
    {
        _pastaTemporaria = Path.Combine(Path.GetTempPath(), $"contabil_backup_teste_{Guid.NewGuid():N}");
        _caminhoBancoOrigem = Path.Combine(_pastaTemporaria, "origem", "contabil.db");
        _diretorioAnexos = Path.Combine(_pastaTemporaria, "anexos");
        _pastaDestino = Path.Combine(_pastaTemporaria, "destino");

        Directory.CreateDirectory(Path.GetDirectoryName(_caminhoBancoOrigem)!);
        Directory.CreateDirectory(_diretorioAnexos);
    }

    public void Dispose()
    {
        if (Directory.Exists(_pastaTemporaria))
            Directory.Delete(_pastaTemporaria, recursive: true);
    }

    private void CriarBancoOrigemComDados()
    {
        // Pooling=False para o handle do arquivo ser liberado de verdade no Dispose() do
        // contexto, e não ficar retido no pool de conexões do Microsoft.Data.Sqlite (mesmo
        // problema documentado em BackupArquivoService.CopiarBancoDeDadosDeFormaSegura).
        var options = new DbContextOptionsBuilder<LivroCaixaDbContext>()
            .UseSqlite($"Data Source={_caminhoBancoOrigem};Pooling=False")
            .Options;

        using var context = new LivroCaixaDbContext(options);
        context.Database.EnsureCreated();

        var empresa = new Empresa("Sindicato Teste", "12.345.678/0001-90");
        var conta = new Conta(empresa.Id, "Caixa", TipoConta.Caixa, 500m);
        context.Empresas.Add(empresa);
        context.Contas.Add(conta);
        context.SaveChanges();
    }

    [Fact]
    public async Task Deve_gerar_zip_com_banco_de_dados_consistente()
    {
        CriarBancoOrigemComDados();
        var servico = new BackupArquivoService();

        var caminhoZip = await servico.CriarBackupAsync(_pastaDestino, _caminhoBancoOrigem, _diretorioAnexos);

        Assert.True(File.Exists(caminhoZip));

        var pastaExtracao = Path.Combine(_pastaTemporaria, "extraido");
        ZipFile.ExtractToDirectory(caminhoZip, pastaExtracao);
        var caminhoBancoExtraido = Path.Combine(pastaExtracao, "contabil.db");
        Assert.True(File.Exists(caminhoBancoExtraido));

        // Reabre o banco EXTRAÍDO do zip (não o de origem) para confirmar que os dados
        // realmente foram copiados de forma consistente pelo backup a quente do SQLite.
        var optionsExtraido = new DbContextOptionsBuilder<LivroCaixaDbContext>()
            .UseSqlite($"Data Source={caminhoBancoExtraido};Pooling=False")
            .Options;
        using var contextExtraido = new LivroCaixaDbContext(optionsExtraido);
        var empresaRecuperada = await contextExtraido.Empresas.SingleAsync();
        Assert.Equal("Sindicato Teste", empresaRecuperada.RazaoSocial);
        var contaRecuperada = await contextExtraido.Contas.SingleAsync();
        Assert.Equal(500m, contaRecuperada.SaldoInicial);
    }

    [Fact]
    public async Task Deve_incluir_anexos_no_zip_preservando_estrutura_de_pastas()
    {
        CriarBancoOrigemComDados();
        var idLancamentoFake = Guid.NewGuid().ToString();
        var pastaAnexoLancamento = Path.Combine(_diretorioAnexos, idLancamentoFake);
        Directory.CreateDirectory(pastaAnexoLancamento);
        await File.WriteAllTextAsync(Path.Combine(pastaAnexoLancamento, "comprovante.pdf"), "conteúdo de teste");

        var servico = new BackupArquivoService();
        var caminhoZip = await servico.CriarBackupAsync(_pastaDestino, _caminhoBancoOrigem, _diretorioAnexos);

        using var arquivoZip = ZipFile.OpenRead(caminhoZip);
        var entrada = arquivoZip.GetEntry($"Anexos/{idLancamentoFake}/comprovante.pdf");
        Assert.NotNull(entrada);
    }
}
