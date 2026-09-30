using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LivroCaixa.Application;
using LivroCaixa.Application.DTOs;
using LivroCaixa.Application.Services;
using LivroCaixa.Domain.Exceptions;

namespace LivroCaixa.App.ViewModels;

public partial class CadastroInicialViewModel : ObservableObject
{
    private readonly CadastroInicialService _cadastroInicialService;
    private readonly SessaoAtual _sessaoAtual;
    private readonly InicializacaoBanco _inicializacaoBanco;

    [ObservableProperty]
    private string razaoSocial = string.Empty;

    [ObservableProperty]
    private string cnpj = string.Empty;

    [ObservableProperty]
    private string? nomeFantasia;

    [ObservableProperty]
    private string? segmento;

    public IReadOnlyList<string> SegmentosDisponiveis { get; } = CategoriasPadrao.Segmentos.Todos;

    [ObservableProperty]
    private string nomeUsuario = string.Empty;

    [ObservableProperty]
    private string mensagemErro = string.Empty;

    [ObservableProperty]
    private bool estaSalvando;

    public string Senha { get; set; } = string.Empty;

    public string ConfirmarSenha { get; set; } = string.Empty;

    public event EventHandler? CadastroConcluido;

    public IAsyncRelayCommand CadastrarCommand { get; }

    public CadastroInicialViewModel(CadastroInicialService cadastroInicialService, SessaoAtual sessaoAtual,
        InicializacaoBanco inicializacaoBanco)
    {
        _inicializacaoBanco = inicializacaoBanco;
        _cadastroInicialService = cadastroInicialService;
        _sessaoAtual = sessaoAtual;
        CadastrarCommand = new AsyncRelayCommand(CadastrarAsync);
    }

    private async Task CadastrarAsync()
    {
        MensagemErro = string.Empty;

        if (Senha != ConfirmarSenha)
        {
            MensagemErro = "As senhas não coincidem.";
            return;
        }

        EstaSalvando = true;
        try
        {
            // Esta tela só abre depois do aceite do termo de uso (ver App.OnStartup).
            var dto = new CadastroInicialDto(RazaoSocial, Cnpj, NomeFantasia, Segmento, NomeUsuario, Senha, TermoDeUso.VersaoAtual);
            await _inicializacaoBanco.AguardarAsync();
            var usuarioCriado = await _cadastroInicialService.CadastrarAsync(dto);

            var nomeExibicao = string.IsNullOrWhiteSpace(NomeFantasia) ? RazaoSocial : NomeFantasia;
            _sessaoAtual.Definir(usuarioCriado.EmpresaId, nomeExibicao, usuarioCriado.Id, usuarioCriado.Nome);
            CadastroConcluido?.Invoke(this, EventArgs.Empty);
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
