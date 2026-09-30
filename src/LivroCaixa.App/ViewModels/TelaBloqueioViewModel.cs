using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LivroCaixa.Application;
using LivroCaixa.Application.Services;
using LivroCaixa.Domain.Exceptions;

namespace LivroCaixa.App.ViewModels;

public partial class TelaBloqueioViewModel : ObservableObject
{
    private readonly SegurancaService _segurancaService;
    private readonly SessaoAtual _sessaoAtual;

    public string NomeUsuario => _sessaoAtual.NomeUsuario;

    public string Senha { get; set; } = string.Empty;

    [ObservableProperty]
    private string mensagemErro = string.Empty;

    [ObservableProperty]
    private bool estaVerificando;

    public event EventHandler? Desbloqueado;

    public event EventHandler? SairSolicitado;

    public IAsyncRelayCommand DesbloquearCommand { get; }

    public IRelayCommand SairCommand { get; }

    public TelaBloqueioViewModel(SegurancaService segurancaService, SessaoAtual sessaoAtual)
    {
        _segurancaService = segurancaService;
        _sessaoAtual = sessaoAtual;
        DesbloquearCommand = new AsyncRelayCommand(DesbloquearAsync);
        SairCommand = new RelayCommand(() => SairSolicitado?.Invoke(this, EventArgs.Empty));
    }

    private async Task DesbloquearAsync()
    {
        MensagemErro = string.Empty;
        EstaVerificando = true;
        try
        {
            if (await _segurancaService.ConfirmarSenhaAsync(_sessaoAtual.UsuarioId, Senha))
                Desbloqueado?.Invoke(this, EventArgs.Empty);
            else
                MensagemErro = "Senha incorreta.";
        }
        catch (DomainException ex)
        {
            MensagemErro = ex.Message;
        }
        finally
        {
            EstaVerificando = false;
        }
    }
}
