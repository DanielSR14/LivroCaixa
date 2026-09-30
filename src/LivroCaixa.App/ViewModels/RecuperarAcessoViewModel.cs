using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LivroCaixa.Application.Services;
using LivroCaixa.Domain.Exceptions;

namespace LivroCaixa.App.ViewModels;

public partial class RecuperarAcessoViewModel : ObservableObject
{
    private readonly SegurancaService _segurancaService;
    private readonly InicializacaoBanco _inicializacaoBanco;

    [ObservableProperty]
    private string nomeUsuario = string.Empty;

    [ObservableProperty]
    private string codigoRecuperacao = string.Empty;

    [ObservableProperty]
    private string mensagemErro = string.Empty;

    [ObservableProperty]
    private bool estaProcessando;

    public string NovaSenha { get; set; } = string.Empty;

    public string ConfirmarNovaSenha { get; set; } = string.Empty;

    /// <summary>Senha trocada; o argumento é o código de recuperação NOVO, para a tela exibir ao usuário.</summary>
    public event EventHandler<string>? AcessoRecuperado;

    public IAsyncRelayCommand RecuperarCommand { get; }

    public RecuperarAcessoViewModel(SegurancaService segurancaService, InicializacaoBanco inicializacaoBanco)
    {
        _inicializacaoBanco = inicializacaoBanco;
        _segurancaService = segurancaService;
        RecuperarCommand = new AsyncRelayCommand(RecuperarAsync);
    }

    private async Task RecuperarAsync()
    {
        MensagemErro = string.Empty;

        if (NovaSenha != ConfirmarNovaSenha)
        {
            MensagemErro = "As senhas não coincidem.";
            return;
        }

        EstaProcessando = true;
        try
        {
            await _inicializacaoBanco.AguardarAsync();
            var novoCodigo = await _segurancaService.RecuperarAcessoAsync(NomeUsuario.Trim(), CodigoRecuperacao, NovaSenha);
            AcessoRecuperado?.Invoke(this, novoCodigo);
        }
        catch (DomainException ex)
        {
            MensagemErro = ex.Message;
        }
        finally
        {
            EstaProcessando = false;
        }
    }
}
