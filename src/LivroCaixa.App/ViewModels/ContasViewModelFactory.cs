using LivroCaixa.Application.Services;

namespace LivroCaixa.App.ViewModels;

public class ContasViewModelFactory
{
    private readonly ContaService _contaService;

    public ContasViewModelFactory(ContaService contaService)
    {
        _contaService = contaService;
    }

    public ContasViewModel Criar(Guid empresaId) => new(_contaService, empresaId);
}
