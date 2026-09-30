using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LivroCaixa.Application;
using LivroCaixa.Application.DTOs;
using LivroCaixa.Application.Interfaces;
using LivroCaixa.Application.Services;
using LivroCaixa.Domain.Entities;
using LivroCaixa.Domain.Enums;
using LivroCaixa.Domain.Exceptions;

namespace LivroCaixa.App.ViewModels;

public partial class TransferenciaViewModel : ObservableObject
{
    private readonly TransferenciaService _transferenciaService;
    private readonly IContaRepository _contaRepository;
    private readonly Guid _empresaId;

    public ColecaoObservavel<Conta> Contas { get; } = new();

    [ObservableProperty]
    private Conta? contaOrigem;

    [ObservableProperty]
    private Conta? contaDestino;

    [ObservableProperty]
    private DateTime data = DateTime.Today;

    [ObservableProperty]
    private string valorTexto = string.Empty;

    [ObservableProperty]
    private string? descricao;

    [ObservableProperty]
    private string mensagemErro = string.Empty;

    [ObservableProperty]
    private bool estaSalvando;

    /// <summary>Com menos de duas contas não há o que transferir — a tela explica em vez de mostrar o formulário.</summary>
    public bool TemContasSuficientes => Contas.Count >= 2;

    public event EventHandler? Concluido;

    public IAsyncRelayCommand SalvarCommand { get; }

    public TransferenciaViewModel(TransferenciaService transferenciaService, IContaRepository contaRepository, Guid empresaId)
    {
        _transferenciaService = transferenciaService;
        _contaRepository = contaRepository;
        _empresaId = empresaId;
        SalvarCommand = new AsyncRelayCommand(SalvarAsync);
    }

    public async Task InicializarAsync()
    {
        Contas.SubstituirPor((await _contaRepository.ListarPorEmpresaAsync(_empresaId)).OrderBy(c => c.Nome));
        OnPropertyChanged(nameof(TemContasSuficientes));

        // Caso mais comum sugerido de cara: do banco para o caixa (saque).
        ContaOrigem = Contas.FirstOrDefault(c => c.Tipo == TipoConta.Banco) ?? Contas.FirstOrDefault();
        ContaDestino = Contas.FirstOrDefault(c => c.Id != ContaOrigem?.Id);
    }

    private async Task SalvarAsync()
    {
        MensagemErro = string.Empty;

        if (ContaOrigem is null || ContaDestino is null)
        {
            MensagemErro = "Selecione a conta de origem e a de destino.";
            return;
        }

        if (!ConversorValor.TentarLer(ValorTexto, out var valor))
        {
            MensagemErro = "Informe um valor numérico válido.";
            return;
        }

        EstaSalvando = true;
        try
        {
            await _transferenciaService.RegistrarAsync(_empresaId, new NovaTransferenciaDto(
                ContaOrigem.Id, ContaDestino.Id, DateOnly.FromDateTime(Data), valor, Descricao));
            Concluido?.Invoke(this, EventArgs.Empty);
        }
        catch (DomainException ex)
        {
            MensagemErro = ex.Message;
        }
        finally
        {
            EstaSalvando = false;
        }
    }
}

public class TransferenciaViewModelFactory
{
    private readonly TransferenciaService _transferenciaService;
    private readonly IContaRepository _contaRepository;

    public TransferenciaViewModelFactory(TransferenciaService transferenciaService, IContaRepository contaRepository)
    {
        _transferenciaService = transferenciaService;
        _contaRepository = contaRepository;
    }

    public TransferenciaViewModel Criar(Guid empresaId) => new(_transferenciaService, _contaRepository, empresaId);
}
