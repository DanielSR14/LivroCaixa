using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LivroCaixa.Application;
using LivroCaixa.Application.DTOs;
using LivroCaixa.Application.Services;
using LivroCaixa.Domain.Exceptions;

namespace LivroCaixa.App.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    private readonly AutenticacaoService _autenticacaoService;
    private readonly SessaoAtual _sessaoAtual;
    private readonly InicializacaoBanco _inicializacaoBanco;

    [ObservableProperty]
    private string nomeUsuario = string.Empty;

    [ObservableProperty]
    private string mensagemErro = string.Empty;

    [ObservableProperty]
    private string mensagemInformativa = string.Empty;

    [ObservableProperty]
    private bool estaEntrando;

    [ObservableProperty]
    private bool lembrarUsuario;

    public string Senha { get; set; } = string.Empty;

    public event EventHandler? LoginConcluido;

    public IAsyncRelayCommand EntrarCommand { get; }

    public LoginViewModel(AutenticacaoService autenticacaoService, SessaoAtual sessaoAtual, InicializacaoBanco inicializacaoBanco)
    {
        _inicializacaoBanco = inicializacaoBanco;
        _autenticacaoService = autenticacaoService;
        _sessaoAtual = sessaoAtual;
        EntrarCommand = new AsyncRelayCommand(EntrarAsync);

        var usuarioLembrado = PreferenciasLogin.ObterUsuarioLembrado();
        if (!string.IsNullOrWhiteSpace(usuarioLembrado))
        {
            NomeUsuario = usuarioLembrado;
            LembrarUsuario = true;
        }
    }

    private async Task EntrarAsync()
    {
        MensagemErro = string.Empty;
        MensagemInformativa = string.Empty;
        EstaEntrando = true;
        try
        {
            await _inicializacaoBanco.AguardarAsync();
            var resultado = await _autenticacaoService.AutenticarAsync(new LoginDto(NomeUsuario, Senha));
            if (resultado is null)
            {
                MensagemErro = "Usuário ou senha inválidos.";
                return;
            }

            var (usuario, empresa) = resultado.Value;
            _sessaoAtual.Definir(usuario.EmpresaId, empresa.NomeExibicao, usuario.Id, usuario.Nome);
            PreferenciasLogin.SalvarUsuarioLembrado(LembrarUsuario ? NomeUsuario : null);
            LoginConcluido?.Invoke(this, EventArgs.Empty);
        }
        catch (DomainException ex)
        {
            MensagemErro = ex.Message;
        }
        finally
        {
            EstaEntrando = false;
        }
    }
}
