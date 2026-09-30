using LivroCaixa.Application.Interfaces;
using LivroCaixa.Application.Services;

namespace LivroCaixa.App.ViewModels;

/// <summary>
/// Constrói o LancamentoFormViewModel resolvendo suas dependências via DI e recebendo
/// apenas os parâmetros que dependem do contexto da tela (empresa, lançamento em edição).
/// A conta é escolhida dentro do próprio formulário, não recebida de fora.
/// </summary>
public class LancamentoFormViewModelFactory
{
    private readonly LancamentoService _lancamentoService;
    private readonly ICategoriaRepository _categoriaRepository;
    private readonly IContaRepository _contaRepository;
    private readonly AnexoService _anexoService;
    private readonly ModeloLancamentoService _modeloService;

    public LancamentoFormViewModelFactory(
        LancamentoService lancamentoService,
        ICategoriaRepository categoriaRepository,
        IContaRepository contaRepository,
        AnexoService anexoService,
        ModeloLancamentoService modeloService)
    {
        _lancamentoService = lancamentoService;
        _categoriaRepository = categoriaRepository;
        _contaRepository = contaRepository;
        _anexoService = anexoService;
        _modeloService = modeloService;
    }

    public LancamentoFormViewModel CriarParaNovo(Guid empresaId) =>
        new(_lancamentoService, _categoriaRepository, _contaRepository, _anexoService, _modeloService, empresaId, null);

    public LancamentoFormViewModel CriarParaEdicao(Guid empresaId, Guid lancamentoId) =>
        new(_lancamentoService, _categoriaRepository, _contaRepository, _anexoService, _modeloService, empresaId, lancamentoId);
}
