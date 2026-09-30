using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LivroCaixa.Application;
using LivroCaixa.Application.DTOs;
using LivroCaixa.Application.Services;
using LivroCaixa.Domain.Enums;
using LivroCaixa.Domain.Exceptions;

namespace LivroCaixa.App.ViewModels;

public partial class NovaContaViewModel : ObservableObject
{
    private readonly ContaService _contaService;
    private readonly Guid _empresaId;
    private readonly Guid? _contaIdEmEdicao;

    public bool EstaEditando => _contaIdEmEdicao is not null;

    public string TituloJanela => EstaEditando ? "Editar conta" : "Nova conta";

    public string TextoBotaoSalvar => EstaEditando ? "Salvar alterações" : "Criar conta";

    public IReadOnlyList<ItemBancoSugerido> BancosSugeridos { get; } = BancosConhecidos.Lista;

    [ObservableProperty]
    private TipoConta tipo = TipoConta.Caixa;

    public bool MostrarCamposBanco => Tipo == TipoConta.Banco;

    public bool MostrarNomeSimples => !MostrarCamposBanco;

    // Usado quando Tipo == Banco.
    [ObservableProperty]
    private ItemBancoSugerido? bancoSelecionado;

    [ObservableProperty]
    private string agencia = string.Empty;

    [ObservableProperty]
    private string numeroConta = string.Empty;

    [ObservableProperty]
    private TipoContaBancaria tipoContaBancariaSelecionada = TipoContaBancaria.Corrente;

    // Usado quando Tipo != Banco (Caixa, Cartão, Investimento) — nome livre, ex: "Cofrinho".
    [ObservableProperty]
    private string nome = string.Empty;

    [ObservableProperty]
    private string saldoInicialTexto = "0";

    [ObservableProperty]
    private string mensagemErro = string.Empty;

    [ObservableProperty]
    private bool estaSalvando;

    public event EventHandler? Concluido;

    public IAsyncRelayCommand SalvarCommand { get; }

    public NovaContaViewModel(ContaService contaService, Guid empresaId)
    {
        _contaService = contaService;
        _empresaId = empresaId;

        SalvarCommand = new AsyncRelayCommand(SalvarAsync);
    }

    /// <summary>Modo edição: pré-preenche os campos a partir de uma conta já existente.</summary>
    public NovaContaViewModel(ContaService contaService, Guid empresaId, ContaResumoDto contaExistente)
        : this(contaService, empresaId)
    {
        _contaIdEmEdicao = contaExistente.Id;
        Tipo = contaExistente.Tipo;
        SaldoInicialTexto = ConversorValor.Formatar(contaExistente.SaldoInicial);

        if (contaExistente.Tipo == TipoConta.Banco)
        {
            BancoSelecionado = BancosSugeridos.FirstOrDefault(b => b.Nome == contaExistente.Nome);
            Agencia = contaExistente.Agencia ?? string.Empty;
            NumeroConta = contaExistente.NumeroConta ?? string.Empty;
            TipoContaBancariaSelecionada = contaExistente.TipoContaBancaria ?? TipoContaBancaria.Corrente;
        }
        else
        {
            Nome = contaExistente.Nome;
        }
    }

    partial void OnTipoChanged(TipoConta value)
    {
        OnPropertyChanged(nameof(MostrarCamposBanco));
        OnPropertyChanged(nameof(MostrarNomeSimples));
    }

    private async Task SalvarAsync()
    {
        MensagemErro = string.Empty;

        string nomeFinal;
        string? agenciaFinal = null;
        string? numeroContaFinal = null;
        TipoContaBancaria? tipoContaBancariaFinal = null;

        if (Tipo == TipoConta.Banco)
        {
            if (BancoSelecionado is null)
            {
                MensagemErro = "Selecione um banco.";
                return;
            }

            nomeFinal = BancoSelecionado.Nome;
            agenciaFinal = Agencia;
            numeroContaFinal = NumeroConta;
            tipoContaBancariaFinal = TipoContaBancariaSelecionada;
        }
        else
        {
            if (string.IsNullOrWhiteSpace(Nome))
            {
                MensagemErro = "Informe o nome da conta.";
                return;
            }

            nomeFinal = Nome;
        }

        if (!ConversorValor.TentarLer(SaldoInicialTexto, out var saldoInicial))
        {
            MensagemErro = "Informe um saldo inicial válido.";
            return;
        }

        EstaSalvando = true;
        try
        {
            if (_contaIdEmEdicao is Guid contaId)
            {
                var dtoEdicao = new AtualizarContaDto(contaId, nomeFinal, Tipo, saldoInicial, agenciaFinal, numeroContaFinal, tipoContaBancariaFinal);
                await _contaService.AtualizarContaAsync(dtoEdicao);
            }
            else
            {
                var dto = new NovaContaDto(nomeFinal, Tipo, saldoInicial, agenciaFinal, numeroContaFinal, tipoContaBancariaFinal);
                await _contaService.CriarContaAsync(_empresaId, dto);
            }

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
