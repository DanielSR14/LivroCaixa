using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LivroCaixa.Application.DTOs;
using LivroCaixa.Application.Services;
using LivroCaixa.Domain.Exceptions;

namespace LivroCaixa.App.ViewModels;

public partial class ContasViewModel : ObservableObject
{
    private readonly ContaService _contaService;
    private readonly Guid _empresaId;

    public ObservableCollection<ContaResumoDto> Contas { get; } = new();

    [ObservableProperty]
    private string mensagemErro = string.Empty;

    public event EventHandler? SolicitarNovaConta;

    public event EventHandler<ContaResumoDto>? SolicitarEditarConta;

    public IAsyncRelayCommand CarregarCommand { get; }

    public IRelayCommand NovaContaCommand { get; }

    public IRelayCommand<ContaResumoDto> EditarContaCommand { get; }

    public IAsyncRelayCommand<ContaResumoDto> ExcluirContaCommand { get; }

    public ContasViewModel(ContaService contaService, Guid empresaId)
    {
        _contaService = contaService;
        _empresaId = empresaId;

        CarregarCommand = new AsyncRelayCommand(CarregarAsync);
        NovaContaCommand = new RelayCommand(() => SolicitarNovaConta?.Invoke(this, EventArgs.Empty));
        EditarContaCommand = new RelayCommand<ContaResumoDto>(conta =>
        {
            if (conta is not null)
                SolicitarEditarConta?.Invoke(this, conta);
        });
        ExcluirContaCommand = new AsyncRelayCommand<ContaResumoDto>(ExcluirAsync!);
    }

    public Task InicializarAsync() => CarregarAsync();

    private async Task CarregarAsync()
    {
        MensagemErro = string.Empty;
        try
        {
            var contas = await _contaService.ListarComSaldoAsync(_empresaId);
            Contas.Clear();
            foreach (var conta in contas)
                Contas.Add(conta);
        }
        catch (DomainException ex)
        {
            MensagemErro = ex.Message;
        }
    }

    private async Task ExcluirAsync(ContaResumoDto conta)
    {
        MensagemErro = string.Empty;
        try
        {
            await _contaService.ExcluirContaAsync(conta.Id);
            await CarregarAsync();
        }
        catch (DomainException ex)
        {
            MensagemErro = ex.Message;
        }
    }

    public Task RecarregarAsync() => CarregarAsync();
}
