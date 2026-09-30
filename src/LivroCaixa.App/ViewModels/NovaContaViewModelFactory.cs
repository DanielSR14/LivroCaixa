using LivroCaixa.Application.DTOs;
using LivroCaixa.Application.Services;

namespace LivroCaixa.App.ViewModels;

public class NovaContaViewModelFactory
{
    private readonly ContaService _contaService;

    public NovaContaViewModelFactory(ContaService contaService)
    {
        _contaService = contaService;
    }

    public NovaContaViewModel CriarParaNovo(Guid empresaId) => new(_contaService, empresaId);

    public NovaContaViewModel CriarParaEdicao(Guid empresaId, ContaResumoDto contaExistente) =>
        new(_contaService, empresaId, contaExistente);
}
