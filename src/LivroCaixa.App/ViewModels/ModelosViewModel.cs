using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LivroCaixa.Application.DTOs;
using LivroCaixa.Application.Services;
using LivroCaixa.Domain.Exceptions;

namespace LivroCaixa.App.ViewModels;

public partial class ModelosViewModel : ObservableObject
{
    private readonly ModeloLancamentoService _modeloService;
    private readonly Guid _empresaId;

    public ColecaoObservavel<ModeloLancamentoDto> Modelos { get; } = new();

    [ObservableProperty]
    private string mensagemErro = string.Empty;

    public event EventHandler? SolicitarNovoModelo;

    public event EventHandler<ModeloLancamentoDto>? SolicitarEditarModelo;

    public IRelayCommand NovoModeloCommand { get; }

    public IRelayCommand<ModeloLancamentoDto> EditarModeloCommand { get; }

    public IAsyncRelayCommand<ModeloLancamentoDto> ExcluirModeloCommand { get; }

    public ModelosViewModel(ModeloLancamentoService modeloService, Guid empresaId)
    {
        _modeloService = modeloService;
        _empresaId = empresaId;

        NovoModeloCommand = new RelayCommand(() => SolicitarNovoModelo?.Invoke(this, EventArgs.Empty));
        EditarModeloCommand = new RelayCommand<ModeloLancamentoDto>(modelo =>
        {
            if (modelo is not null)
                SolicitarEditarModelo?.Invoke(this, modelo);
        });
        ExcluirModeloCommand = new AsyncRelayCommand<ModeloLancamentoDto>(ExcluirAsync!);
    }

    public async Task CarregarAsync()
    {
        MensagemErro = string.Empty;
        try
        {
            Modelos.SubstituirPor(await _modeloService.ListarAsync(_empresaId));
        }
        catch (DomainException ex)
        {
            MensagemErro = ex.Message;
        }
    }

    private async Task ExcluirAsync(ModeloLancamentoDto modelo)
    {
        MensagemErro = string.Empty;
        try
        {
            await _modeloService.ExcluirAsync(modelo.Id);
            await CarregarAsync();
        }
        catch (DomainException ex)
        {
            MensagemErro = ex.Message;
        }
    }
}

public class ModelosViewModelFactory
{
    private readonly ModeloLancamentoService _modeloService;

    public ModelosViewModelFactory(ModeloLancamentoService modeloService) => _modeloService = modeloService;

    public ModelosViewModel Criar(Guid empresaId) => new(_modeloService, empresaId);
}
