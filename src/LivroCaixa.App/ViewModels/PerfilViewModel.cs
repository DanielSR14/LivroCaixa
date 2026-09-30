using CommunityToolkit.Mvvm.ComponentModel;
using LivroCaixa.Application;
using LivroCaixa.Application.Services;
using LivroCaixa.Domain.Enums;
using LivroCaixa.Domain.Exceptions;

namespace LivroCaixa.App.ViewModels;

public partial class PerfilViewModel : ObservableObject
{
    private readonly PerfilService _perfilService;
    private readonly SessaoAtual _sessaoAtual;

    [ObservableProperty]
    private string nomeUsuario = string.Empty;

    /// <summary>Iniciais do usuário pro avatar circular (ex: "Maria Silva" → "MS").</summary>
    public string IniciaisUsuario
    {
        get
        {
            var partes = NomeUsuario.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return partes.Length switch
            {
                0 => "?",
                1 => partes[0][..1].ToUpperInvariant(),
                _ => $"{partes[0][0]}{partes[^1][0]}".ToUpperInvariant()
            };
        }
    }

    [ObservableProperty]
    private string perfilExibicao = string.Empty;

    [ObservableProperty]
    private string nomeEmpresa = string.Empty;

    [ObservableProperty]
    private string razaoSocial = string.Empty;

    [ObservableProperty]
    private string cnpj = string.Empty;

    [ObservableProperty]
    private string? segmento;

    [ObservableProperty]
    private DateTime dataCadastroEmpresa;

    [ObservableProperty]
    private string mensagemErro = string.Empty;

    public PerfilViewModel(PerfilService perfilService, SessaoAtual sessaoAtual)
    {
        _perfilService = perfilService;
        _sessaoAtual = sessaoAtual;
    }

    partial void OnNomeUsuarioChanged(string value) => OnPropertyChanged(nameof(IniciaisUsuario));

    public async Task InicializarAsync()
    {
        try
        {
            var perfil = await _perfilService.ObterAsync(_sessaoAtual.UsuarioId);
            NomeUsuario = perfil.NomeUsuario;
            PerfilExibicao = perfil.PerfilUsuario switch
            {
                PerfilUsuario.Administrador => "Administrador",
                PerfilUsuario.Funcionario => "Funcionário",
                PerfilUsuario.Visualizacao => "Visualização",
                _ => perfil.PerfilUsuario.ToString()
            };
            NomeEmpresa = perfil.NomeExibicaoEmpresa;
            RazaoSocial = perfil.RazaoSocial;
            Cnpj = perfil.Cnpj;
            Segmento = perfil.Segmento;
            DataCadastroEmpresa = perfil.DataCadastroEmpresa;
        }
        catch (DomainException ex)
        {
            MensagemErro = ex.Message;
        }
    }
}
