using LivroCaixa.Application;
using LivroCaixa.Application.Services;

namespace LivroCaixa.App.ViewModels;

public class ExportacaoViewModelFactory
{
    private readonly ExportacaoService _exportacaoService;
    private readonly SessaoAtual _sessaoAtual;

    public ExportacaoViewModelFactory(ExportacaoService exportacaoService, SessaoAtual sessaoAtual)
    {
        _exportacaoService = exportacaoService;
        _sessaoAtual = sessaoAtual;
    }

    public ExportacaoViewModel Criar(Guid empresaId) => new(_exportacaoService, _sessaoAtual, empresaId);
}
