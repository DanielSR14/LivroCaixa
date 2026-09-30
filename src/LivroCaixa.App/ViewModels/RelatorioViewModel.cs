using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LivroCaixa.Application;
using LivroCaixa.Application.DTOs;
using LivroCaixa.Application.Services;
using LivroCaixa.Domain.Exceptions;

namespace LivroCaixa.App.ViewModels;

public partial class RelatorioViewModel : ObservableObject
{
    private readonly RelatorioService _relatorioService;
    private readonly ContaService _contaService;
    private readonly SessaoAtual _sessaoAtual;

    public Guid EmpresaId => _sessaoAtual.EmpresaId;

    public static IReadOnlyList<ItemMes> MesesDisponiveis => ExportacaoViewModel.MesesDisponiveis;

    public IReadOnlyList<int> AnosDisponiveis { get; } =
        Enumerable.Range(DateTime.Today.Year - 4, 5).Reverse().ToList();

    [ObservableProperty]
    private int ano = DateTime.Today.Year;

    [ObservableProperty]
    private int mes = DateTime.Today.Month;

    [ObservableProperty]
    private string mensagemErro = string.Empty;

    [ObservableProperty]
    private bool estaCarregando;

    [ObservableProperty]
    private decimal totalEntradas;

    [ObservableProperty]
    private decimal totalSaidas;

    [ObservableProperty]
    private decimal resultado;

    public ObservableCollection<ItemResultadoCategoria> EntradasPorCategoria { get; } = new();

    public ObservableCollection<ItemResultadoCategoria> SaidasPorCategoria { get; } = new();

    public ColecaoObservavel<LancamentoResumoDto> Extrato { get; } = new();

    public ObservableCollection<SaldoMensalItem> FluxoDeCaixa { get; } = new();

    public ObservableCollection<ItemContaFiltro> ContasDisponiveis { get; } = new();

    [ObservableProperty]
    private ItemContaFiltro? contaSelecionada;

    // Comparação com outra competência: opcional, ligada pelo checkbox "Comparar com outro mês".
    [ObservableProperty]
    private bool compararAtivo;

    [ObservableProperty]
    private int anoComparacao = DateTime.Today.AddMonths(-1).Year;

    [ObservableProperty]
    private int mesComparacao = DateTime.Today.AddMonths(-1).Month;

    [ObservableProperty]
    private decimal totalEntradasComparacao;

    [ObservableProperty]
    private decimal totalSaidasComparacao;

    [ObservableProperty]
    private decimal resultadoComparacao;

    public string CompetenciaPrincipalExibicao => new DateTime(Ano, Mes, 1).ToString("MMMM/yyyy");

    public string CompetenciaComparacaoExibicao => new DateTime(AnoComparacao, MesComparacao, 1).ToString("MMMM/yyyy");

    public IAsyncRelayCommand GerarCommand { get; }

    /// <summary>
    /// Ligado durante <see cref="InicializarAsync"/>: selecionar a conta padrão dispara
    /// OnContaSelecionadaChanged, e sem isso o relatório era gerado duas vezes na abertura.
    /// </summary>
    private bool _inicializando;

    public RelatorioViewModel(RelatorioService relatorioService, ContaService contaService, SessaoAtual sessaoAtual)
    {
        _relatorioService = relatorioService;
        _contaService = contaService;
        _sessaoAtual = sessaoAtual;

        GerarCommand = new AsyncRelayCommand(GerarAsync);
    }

    public async Task InicializarAsync()
    {
        _inicializando = true;
        try
        {
            await CarregarContasAsync();
        }
        finally
        {
            _inicializando = false;
        }

        await GerarAsync();
    }

    private void Regerar()
    {
        if (!_inicializando)
            GerarCommand.Execute(null);
    }

    partial void OnAnoChanged(int value) => Regerar();

    partial void OnMesChanged(int value) => Regerar();

    partial void OnContaSelecionadaChanged(ItemContaFiltro? value) => Regerar();

    partial void OnCompararAtivoChanged(bool value) => Regerar();

    partial void OnAnoComparacaoChanged(int value)
    {
        OnPropertyChanged(nameof(CompetenciaComparacaoExibicao));
        if (CompararAtivo)
            Regerar();
    }

    partial void OnMesComparacaoChanged(int value)
    {
        OnPropertyChanged(nameof(CompetenciaComparacaoExibicao));
        if (CompararAtivo)
            Regerar();
    }

    private async Task CarregarContasAsync()
    {
        var contas = await _contaService.ListarComSaldoAsync(EmpresaId);

        ContasDisponiveis.Clear();
        ContasDisponiveis.Add(new ItemContaFiltro(null, "Todas as contas"));
        foreach (var conta in contas)
            ContasDisponiveis.Add(new ItemContaFiltro(conta.Id, conta.Nome));

        ContaSelecionada = ContasDisponiveis[0];
    }

    private async Task GerarAsync()
    {
        MensagemErro = string.Empty;
        EstaCarregando = true;
        try
        {
            OnPropertyChanged(nameof(CompetenciaPrincipalExibicao));

            var contaId = ContaSelecionada?.Id;
            var relatorio = await _relatorioService.GerarRelatorioPeriodoAsync(EmpresaId, Ano, Mes, contaId);
            TotalEntradas = relatorio.TotalEntradas;
            TotalSaidas = relatorio.TotalSaidas;
            Resultado = relatorio.Resultado;

            AtualizarCategorias(EntradasPorCategoria, relatorio.EntradasPorCategoria);
            AtualizarCategorias(SaidasPorCategoria, relatorio.SaidasPorCategoria);

            Extrato.SubstituirPor(relatorio.Extrato);

            var saldos = await _relatorioService.ObterSaldoAcumuladoUltimosMesesAsync(EmpresaId);
            AtualizarFluxoDeCaixa(saldos);

            if (CompararAtivo)
            {
                var relatorioComparacao = await _relatorioService.GerarRelatorioPeriodoAsync(EmpresaId, AnoComparacao, MesComparacao, contaId);
                TotalEntradasComparacao = relatorioComparacao.TotalEntradas;
                TotalSaidasComparacao = relatorioComparacao.TotalSaidas;
                ResultadoComparacao = relatorioComparacao.Resultado;
            }
        }
        catch (DomainException ex)
        {
            MensagemErro = ex.Message;
        }
        finally
        {
            EstaCarregando = false;
        }
    }

    private static void AtualizarCategorias(ObservableCollection<ItemResultadoCategoria> destino, IReadOnlyList<ResultadoCategoriaDto> origem)
    {
        destino.Clear();
        var maiorValor = origem.Count > 0 ? origem.Max(o => o.Total) : 0m;
        foreach (var item in origem)
        {
            var percentual = maiorValor > 0 ? (double)(item.Total / maiorValor) * 100.0 : 0.0;
            destino.Add(new ItemResultadoCategoria(item.Categoria, item.Total, percentual));
        }
    }

    private void AtualizarFluxoDeCaixa(IReadOnlyList<SaldoMensalDto> saldos)
    {
        FluxoDeCaixa.Clear();
        if (saldos.Count == 0)
            return;

        var maiorValor = saldos.Max(s => Math.Abs(s.SaldoFinal));
        foreach (var saldo in saldos)
        {
            var mesExibicao = new DateTime(saldo.Ano, saldo.Mes, 1).ToString("MMM/yy");
            var percentual = maiorValor > 0 ? (double)(Math.Abs(saldo.SaldoFinal) / maiorValor) * 100.0 : 0.0;
            FluxoDeCaixa.Add(new SaldoMensalItem(mesExibicao, saldo.SaldoFinal, percentual));
        }
    }
}
