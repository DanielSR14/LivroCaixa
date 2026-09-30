using LivroCaixa.Application.Services;

namespace LivroCaixa.App.ViewModels;

public class LancamentoDetalheViewModelFactory
{
    private readonly LancamentoService _lancamentoService;
    private readonly AnexoService _anexoService;

    public LancamentoDetalheViewModelFactory(LancamentoService lancamentoService, AnexoService anexoService)
    {
        _lancamentoService = lancamentoService;
        _anexoService = anexoService;
    }

    public async Task<LancamentoDetalheViewModel> CriarAsync(Guid lancamentoId)
    {
        var detalhe = await _lancamentoService.ObterDetalheExibicaoAsync(lancamentoId);
        var historico = await _lancamentoService.ListarHistoricoAsync(lancamentoId);
        return new LancamentoDetalheViewModel(_anexoService, lancamentoId, detalhe, historico);
    }
}
