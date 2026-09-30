using LivroCaixa.Application.Services;

namespace LivroCaixa.App.ViewModels;

public class ArmazenamentoViewModelFactory
{
    private readonly ArmazenamentoService _armazenamentoService;

    public ArmazenamentoViewModelFactory(ArmazenamentoService armazenamentoService)
    {
        _armazenamentoService = armazenamentoService;
    }

    public ArmazenamentoViewModel Criar(Guid empresaId) => new(_armazenamentoService, empresaId);
}
