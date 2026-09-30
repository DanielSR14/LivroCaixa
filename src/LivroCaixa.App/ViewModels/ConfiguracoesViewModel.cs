using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LivroCaixa.Application.Services;
using LivroCaixa.Domain.Entities;
using LivroCaixa.Domain.Enums;
using LivroCaixa.Domain.Exceptions;

namespace LivroCaixa.App.ViewModels;

public partial class ConfiguracoesViewModel : ObservableObject
{
    private readonly BackupService _backupService;
    private readonly SegurancaService _segurancaService;
    private readonly Guid _empresaId;
    private readonly Guid _usuarioId;

    public static IReadOnlyList<ItemFrequenciaBackup> FrequenciasDisponiveis { get; } = new List<ItemFrequenciaBackup>
    {
        new(FrequenciaBackup.Diaria, "Diária"),
        new(FrequenciaBackup.Semanal, "Semanal"),
        new(FrequenciaBackup.Mensal, "Mensal")
    };

    public static IReadOnlyList<ItemTempoBloqueio> OpcoesBloqueioInatividade { get; } =
        Empresa.OpcoesBloqueioInatividadeMinutos.Select(ItemTempoBloqueio.De).ToList();

    [ObservableProperty]
    private int minutosBloqueioInatividade = Empresa.BloqueioInatividadePadraoMinutos;

    [ObservableProperty]
    private bool backupAtivo;

    [ObservableProperty]
    private FrequenciaBackup frequenciaSelecionada = FrequenciaBackup.Semanal;

    [ObservableProperty]
    private string? pastaDestino;

    [ObservableProperty]
    private DateTime? ultimoBackupEm;

    [ObservableProperty]
    private string proximoBackupExibicao = string.Empty;

    [ObservableProperty]
    private string mensagemErro = string.Empty;

    [ObservableProperty]
    private string mensagemStatus = string.Empty;

    [ObservableProperty]
    private bool estaProcessando;

    public IAsyncRelayCommand SalvarCommand { get; }

    public IAsyncRelayCommand FazerBackupAgoraCommand { get; }

    public ConfiguracoesViewModel(BackupService backupService, SegurancaService segurancaService, Guid empresaId, Guid usuarioId)
    {
        _backupService = backupService;
        _segurancaService = segurancaService;
        _empresaId = empresaId;
        _usuarioId = usuarioId;

        SalvarCommand = new AsyncRelayCommand(SalvarAsync);
        FazerBackupAgoraCommand = new AsyncRelayCommand(FazerBackupAgoraAsync);
    }

    public Task InicializarAsync() => AtualizarEstadoAsync();

    public void DefinirPastaDestino(string pasta) => PastaDestino = pasta;

    /// <summary>Chamado pelo code-behind só depois de o usuário confirmar que guardou o código exibido.</summary>
    public Task SalvarCodigoRecuperacaoAsync(string codigo) => _segurancaService.DefinirCodigoRecuperacaoAsync(_usuarioId, codigo);

    private async Task AtualizarEstadoAsync()
    {
        MinutosBloqueioInatividade = await _segurancaService.ObterMinutosBloqueioInatividadeAsync();

        var configuracao = await _backupService.ObterConfiguracaoAsync(_empresaId);
        BackupAtivo = configuracao.Ativo;
        FrequenciaSelecionada = configuracao.Frequencia;
        PastaDestino = configuracao.PastaDestino;
        UltimoBackupEm = configuracao.UltimoBackupEm;
        ProximoBackupExibicao = FormatarProximoBackup(configuracao.ProximoBackupEm);
    }

    private static string FormatarProximoBackup(DateTime? proximoBackupEm)
    {
        if (proximoBackupEm is null)
            return "Backup automático desativado.";

        if (proximoBackupEm == DateTime.MinValue)
            return "Assim que o app for aberto novamente.";

        return proximoBackupEm.Value.ToString("dddd, dd/MM/yyyy 'às' HH:mm");
    }

    private async Task SalvarAsync()
    {
        MensagemErro = string.Empty;
        MensagemStatus = string.Empty;
        try
        {
            await _segurancaService.DefinirMinutosBloqueioInatividadeAsync(MinutosBloqueioInatividade);
            await _backupService.ConfigurarAsync(_empresaId, BackupAtivo, FrequenciaSelecionada, PastaDestino);
            await AtualizarEstadoAsync();
            MensagemStatus = "Configurações salvas.";
        }
        catch (DomainException ex)
        {
            MensagemErro = ex.Message;
        }
    }

    private async Task FazerBackupAgoraAsync()
    {
        MensagemErro = string.Empty;
        MensagemStatus = string.Empty;

        if (string.IsNullOrWhiteSpace(PastaDestino))
        {
            MensagemErro = "Escolha uma pasta de destino antes de fazer o backup.";
            return;
        }

        EstaProcessando = true;
        try
        {
            var caminhoGerado = await _backupService.FazerBackupManualAsync(_empresaId, PastaDestino);
            await AtualizarEstadoAsync();
            MensagemStatus = $"Backup salvo em: {caminhoGerado}";
        }
        catch (Exception ex)
        {
            MensagemErro = $"Não foi possível fazer o backup: {ex.Message}";
        }
        finally
        {
            EstaProcessando = false;
        }
    }
}
