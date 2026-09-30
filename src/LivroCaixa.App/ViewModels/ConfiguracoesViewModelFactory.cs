using LivroCaixa.Application.Services;

namespace LivroCaixa.App.ViewModels;

public class ConfiguracoesViewModelFactory
{
    private readonly BackupService _backupService;
    private readonly SegurancaService _segurancaService;

    public ConfiguracoesViewModelFactory(BackupService backupService, SegurancaService segurancaService)
    {
        _backupService = backupService;
        _segurancaService = segurancaService;
    }

    public ConfiguracoesViewModel Criar(Guid empresaId, Guid usuarioId) =>
        new(_backupService, _segurancaService, empresaId, usuarioId);
}
