using LivroCaixa.Application.DTOs;
using LivroCaixa.Application.Interfaces;
using LivroCaixa.Domain.Entities;
using LivroCaixa.Domain.Enums;

namespace LivroCaixa.Application.Services;

/// <summary>
/// Backup manual e automático (banco + comprovantes num .zip). A regra de "já está na hora"
/// fica no Domain (ConfiguracaoBackup.PrecisaFazerBackupAgora); aqui só se orquestra.
/// </summary>
public class BackupService
{
    private readonly IConfiguracaoBackupRepository _configuracaoBackupRepository;
    private readonly IBackupArquivoService _backupArquivoService;
    private readonly IUnitOfWork _unitOfWork;

    public BackupService(
        IConfiguracaoBackupRepository configuracaoBackupRepository,
        IBackupArquivoService backupArquivoService,
        IUnitOfWork unitOfWork)
    {
        _configuracaoBackupRepository = configuracaoBackupRepository;
        _backupArquivoService = backupArquivoService;
        _unitOfWork = unitOfWork;
    }

    public async Task<ConfiguracaoBackupDto> ObterConfiguracaoAsync(Guid empresaId)
    {
        var configuracao = await ObterOuCriarAsync(empresaId);
        return Mapear(configuracao);
    }

    public async Task ConfigurarAsync(Guid empresaId, bool ativo, FrequenciaBackup frequencia, string? pastaDestino)
    {
        var configuracao = await ObterOuCriarAsync(empresaId);
        configuracao.Configurar(ativo, frequencia, pastaDestino);
        await _unitOfWork.SalvarAlteracoesAsync();
    }

    public async Task<string> FazerBackupManualAsync(Guid empresaId, string pastaDestino)
    {
        var caminhoGerado = await _backupArquivoService.CriarBackupAsync(pastaDestino);

        var configuracao = await ObterOuCriarAsync(empresaId);
        configuracao.RegistrarBackupRealizado(DateTime.Now);
        await _unitOfWork.SalvarAlteracoesAsync();

        return caminhoGerado;
    }

    /// <summary>Chamado na abertura do app. Só executa (e só grava) se a configuração estiver ativa e o prazo já tiver vencido.</summary>
    public async Task<string?> ExecutarBackupAutomaticoSeNecessarioAsync(Guid empresaId)
    {
        var configuracao = await ObterOuCriarAsync(empresaId);
        if (!configuracao.PrecisaFazerBackupAgora(DateTime.Now))
            return null;

        var caminhoGerado = await _backupArquivoService.CriarBackupAsync(configuracao.PastaDestino!);
        configuracao.RegistrarBackupRealizado(DateTime.Now);
        await _unitOfWork.SalvarAlteracoesAsync();

        return caminhoGerado;
    }

    private async Task<ConfiguracaoBackup> ObterOuCriarAsync(Guid empresaId)
    {
        var configuracao = await _configuracaoBackupRepository.ObterPorEmpresaAsync(empresaId);
        if (configuracao is not null)
            return configuracao;

        configuracao = new ConfiguracaoBackup(empresaId);
        await _configuracaoBackupRepository.AdicionarAsync(configuracao);
        await _unitOfWork.SalvarAlteracoesAsync();
        return configuracao;
    }

    private static ConfiguracaoBackupDto Mapear(ConfiguracaoBackup configuracao) => new(
        configuracao.Ativo, configuracao.Frequencia, configuracao.PastaDestino, configuracao.UltimoBackupEm,
        configuracao.ObterProximoBackupEm());
}
