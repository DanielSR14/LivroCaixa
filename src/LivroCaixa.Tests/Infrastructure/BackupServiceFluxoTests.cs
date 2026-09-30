using LivroCaixa.Application.Interfaces;
using LivroCaixa.Application.Services;
using LivroCaixa.Domain.Entities;
using LivroCaixa.Domain.Enums;
using LivroCaixa.Infrastructure.Database;
using LivroCaixa.Infrastructure.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace LivroCaixa.Tests.Infrastructure;

/// <summary>
/// Testa a persistência real (SQLite) da configuração de backup — a mecânica de gerar o
/// arquivo em si já é coberta por <see cref="BackupArquivoServiceFluxoTests"/>, então aqui
/// o serviço de arquivo é um fake simples que só confirma que foi chamado.
/// </summary>
public class BackupServiceFluxoTests : IDisposable
{
    private sealed class BackupArquivoServiceFake : IBackupArquivoService
    {
        public int VezesChamado { get; private set; }

        public Task<string> CriarBackupAsync(string pastaDestino)
        {
            VezesChamado++;
            return Task.FromResult(Path.Combine(pastaDestino, "backup-fake.zip"));
        }
    }

    private readonly SqliteConnection _connection;
    private readonly LivroCaixaDbContext _context;
    private readonly BackupArquivoServiceFake _arquivoFake = new();
    private readonly BackupService _servico;
    private readonly Guid _empresaId;

    public BackupServiceFluxoTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<LivroCaixaDbContext>().UseSqlite(_connection).Options;
        _context = new LivroCaixaDbContext(options);
        _context.Database.EnsureCreated();

        var empresa = new Empresa("Sindicato Teste", "12.345.678/0001-90");
        _empresaId = empresa.Id;
        _context.Empresas.Add(empresa);
        _context.SaveChanges();

        var repositorio = new ConfiguracaoBackupRepository(_context);
        _servico = new BackupService(repositorio, _arquivoFake, _context);
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task Primeira_consulta_deve_criar_configuracao_desativada_por_padrao()
    {
        var configuracao = await _servico.ObterConfiguracaoAsync(_empresaId);

        Assert.False(configuracao.Ativo);
        Assert.Equal(FrequenciaBackup.Semanal, configuracao.Frequencia);
        Assert.Null(configuracao.PastaDestino);
    }

    [Fact]
    public async Task Deve_persistir_configuracao_e_recarregar_em_novo_contexto()
    {
        await _servico.ConfigurarAsync(_empresaId, ativo: true, FrequenciaBackup.Diaria, @"C:\Backups");

        using var novoContexto = new LivroCaixaDbContext(
            new DbContextOptionsBuilder<LivroCaixaDbContext>().UseSqlite(_connection).Options);
        var repositorioNovo = new ConfiguracaoBackupRepository(novoContexto);
        var servicoNovo = new BackupService(repositorioNovo, _arquivoFake, novoContexto);

        var configuracao = await servicoNovo.ObterConfiguracaoAsync(_empresaId);
        Assert.True(configuracao.Ativo);
        Assert.Equal(FrequenciaBackup.Diaria, configuracao.Frequencia);
        Assert.Equal(@"C:\Backups", configuracao.PastaDestino);
    }

    [Fact]
    public async Task Nao_deve_rodar_backup_automatico_quando_desativado()
    {
        var caminho = await _servico.ExecutarBackupAutomaticoSeNecessarioAsync(_empresaId);

        Assert.Null(caminho);
        Assert.Equal(0, _arquivoFake.VezesChamado);
    }

    [Fact]
    public async Task Deve_rodar_backup_automatico_quando_ativo_e_nunca_executado_antes()
    {
        await _servico.ConfigurarAsync(_empresaId, ativo: true, FrequenciaBackup.Semanal, @"C:\Backups");

        var caminho = await _servico.ExecutarBackupAutomaticoSeNecessarioAsync(_empresaId);

        Assert.NotNull(caminho);
        Assert.Equal(1, _arquivoFake.VezesChamado);

        var configuracaoAtualizada = await _servico.ObterConfiguracaoAsync(_empresaId);
        Assert.NotNull(configuracaoAtualizada.UltimoBackupEm);
    }

    [Fact]
    public async Task Nao_deve_rodar_backup_automatico_de_novo_antes_do_prazo_vencer()
    {
        await _servico.ConfigurarAsync(_empresaId, ativo: true, FrequenciaBackup.Semanal, @"C:\Backups");
        await _servico.ExecutarBackupAutomaticoSeNecessarioAsync(_empresaId);

        var segundaChamada = await _servico.ExecutarBackupAutomaticoSeNecessarioAsync(_empresaId);

        Assert.Null(segundaChamada);
        Assert.Equal(1, _arquivoFake.VezesChamado);
    }
}
