using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LivroCaixa.Application;
using LivroCaixa.Application.Services;
using LivroCaixa.Domain.Exceptions;

namespace LivroCaixa.App.ViewModels;

public partial class ExportacaoViewModel : ObservableObject
{
    private readonly ExportacaoService _exportacaoService;
    private readonly SessaoAtual _sessaoAtual;
    private readonly Guid _empresaId;

    // Competência: o cliente sempre exporta o mês anterior (ex: em setembro, envia agosto).
    private static readonly DateTime CompetenciaPadrao = DateTime.Today.AddMonths(-1);

    public static IReadOnlyList<ItemMes> MesesDisponiveis { get; } = new List<ItemMes>
    {
        new(1, "Janeiro"), new(2, "Fevereiro"), new(3, "Março"), new(4, "Abril"),
        new(5, "Maio"), new(6, "Junho"), new(7, "Julho"), new(8, "Agosto"),
        new(9, "Setembro"), new(10, "Outubro"), new(11, "Novembro"), new(12, "Dezembro")
    };

    public IReadOnlyList<int> AnosDisponiveis { get; } =
        Enumerable.Range(CompetenciaPadrao.Year - 4, 5).Reverse().ToList();

    [ObservableProperty]
    private int ano = CompetenciaPadrao.Year;

    [ObservableProperty]
    private int mes = CompetenciaPadrao.Month;

    [ObservableProperty]
    private string mensagemErro = string.Empty;

    [ObservableProperty]
    private string? caminhoGerado;

    [ObservableProperty]
    private bool estaExportando;

    public string EmailDestinatario => DadosDoEscritorio.EmailContabil;

    public string EmailAssunto => $"Lançamentos e arquivos para a contabilidade - {_sessaoAtual.NomeEmpresa} comp. {Mes:D2}/{Ano:D4}";

    public string EmailCorpo
    {
        get
        {
            var saudacao = DateTime.Now.Hour switch
            {
                < 12 => "Bom dia",
                < 18 => "Boa tarde",
                _ => "Boa noite"
            };
            return $"{saudacao},\n\nSegue em anexo os lançamentos e comprovantes referentes à competência {Mes:D2}/{Ano:D4} da empresa {_sessaoAtual.NomeEmpresa}.\n\nQualquer dúvida, estou à disposição.";
        }
    }

    public ExportacaoViewModel(ExportacaoService exportacaoService, SessaoAtual sessaoAtual, Guid empresaId)
    {
        _exportacaoService = exportacaoService;
        _sessaoAtual = sessaoAtual;
        _empresaId = empresaId;
        NovaExportacaoCommand = new RelayCommand(NovaExportacao);
    }

    public IRelayCommand NovaExportacaoCommand { get; }

    private void NovaExportacao()
    {
        CaminhoGerado = null;
        MensagemErro = string.Empty;
    }

    public async Task ExportarAsync(string diretorioDestino)
    {
        MensagemErro = string.Empty;
        CaminhoGerado = null;
        EstaExportando = true;
        try
        {
            CaminhoGerado = await _exportacaoService.ExportarAsync(_empresaId, Ano, Mes, diretorioDestino);
            OnPropertyChanged(nameof(EmailAssunto));
            OnPropertyChanged(nameof(EmailCorpo));
        }
        catch (DomainException ex)
        {
            MensagemErro = ex.Message;
        }
        finally
        {
            EstaExportando = false;
        }
    }
}
