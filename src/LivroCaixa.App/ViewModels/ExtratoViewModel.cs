using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LivroCaixa.Application;
using LivroCaixa.Application.DTOs;
using LivroCaixa.Application.Services;
using LivroCaixa.Domain.Enums;
using LivroCaixa.Domain.Exceptions;

namespace LivroCaixa.App.ViewModels;

public partial class ExtratoViewModel : ObservableObject
{
    public const string TodasOpcaoFiltro = "Todas";

    private readonly LancamentoService _lancamentoService;
    private readonly PendenciasService _pendenciasService;
    private readonly TransferenciaService _transferenciaService;
    private readonly SessaoAtual _sessaoAtual;

    public string NomeEmpresa => _sessaoAtual.NomeEmpresa;

    public string NomeUsuario => _sessaoAtual.NomeUsuario;

    public string Saudacao
    {
        get
        {
            var periodo = DateTime.Now.Hour switch
            {
                < 12 => "Bom dia",
                < 18 => "Boa tarde",
                _ => "Boa noite"
            };
            return $"{periodo}, {NomeEmpresa}!";
        }
    }

    public Guid EmpresaId => _sessaoAtual.EmpresaId;

    /// <summary>Ex: "Setembro de 2026" — deixa claro de qual competência são os cards e o gráfico do Dashboard.</summary>
    public string CompetenciaAtualExibicao
    {
        get
        {
            var texto = DateTime.Today.ToString("MMMM 'de' yyyy", CultureInfo.CurrentCulture);
            return char.ToUpper(texto[0], CultureInfo.CurrentCulture) + texto[1..];
        }
    }

    [ObservableProperty]
    private decimal saldoAtual;

    [ObservableProperty]
    private decimal totalEntradasMes;

    [ObservableProperty]
    private decimal totalSaidasMes;

    [ObservableProperty]
    private string mensagemErro = string.Empty;

    /// <summary>Frases prontas para o card "Pendências" do Dashboard (vazio = tudo em dia).</summary>
    public ColecaoObservavel<string> Pendencias { get; } = new();

    [ObservableProperty]
    private bool temPendencias;

    /// <summary>Extrato completo sem filtro — base para <see cref="LancamentosFiltrados"/> (filtragem é client-side).</summary>
    private IReadOnlyList<LancamentoResumoDto> _todosLancamentos = Array.Empty<LancamentoResumoDto>();

    /// <summary>
    /// Ligado enquanto vários filtros mudam de uma vez (limpar filtros, recarga das opções),
    /// para refiltrar uma vez só no final em vez de uma vez por propriedade alterada.
    /// </summary>
    private bool _filtrosSuspensos;

    public ColecaoObservavel<LancamentoResumoDto> LancamentosFiltrados { get; } = new();

    public ObservableCollection<string> ContasParaFiltro { get; } = new();

    public ObservableCollection<string> CategoriasParaFiltro { get; } = new();

    public ColecaoObservavel<LancamentoResumoDto> UltimosLancamentos { get; } = new();

    public ColecaoObservavel<LancamentoResumoDto> Excluidos { get; } = new();

    [ObservableProperty]
    private bool exibirExcluidos;

    public ObservableCollection<FatiaPizza> FatiasSaidasPorCategoria { get; } = new();

    public ObservableCollection<EvolucaoMensalItem> EvolucaoMensal { get; } = new();

    public IReadOnlyList<DiaSemanaItem> DiasDaSemana { get; } = ComputarSemanaAtual();

    [ObservableProperty]
    private string filtroConta = TodasOpcaoFiltro;

    [ObservableProperty]
    private string filtroCategoria = TodasOpcaoFiltro;

    [ObservableProperty]
    private DateTime? filtroDataInicio;

    [ObservableProperty]
    private DateTime? filtroDataFim;

    /// <summary>Busca livre na descrição/observação (ignora maiúsculas e acentos: "agua" acha "Água").</summary>
    [ObservableProperty]
    private string textoBusca = string.Empty;

    public event EventHandler? SolicitarNovoLancamento;

    public event EventHandler<Guid>? SolicitarEdicaoLancamento;

    public event EventHandler? SolicitarGerenciarContas;

    public event EventHandler? SolicitarNovaTransferencia;

    public IAsyncRelayCommand CarregarCommand { get; }

    public IAsyncRelayCommand<Guid> ExcluirLancamentoCommand { get; }

    public IAsyncRelayCommand<Guid> RestaurarLancamentoCommand { get; }

    public IAsyncRelayCommand<Guid> ExcluirTransferenciaCommand { get; }

    public IRelayCommand NovaTransferenciaCommand { get; }

    public IRelayCommand NovoLancamentoCommand { get; }

    public IRelayCommand<Guid> EditarLancamentoCommand { get; }

    public IRelayCommand GerenciarContasCommand { get; }

    public IRelayCommand LimparFiltrosCommand { get; }

    public ExtratoViewModel(
        LancamentoService lancamentoService,
        PendenciasService pendenciasService,
        TransferenciaService transferenciaService,
        SessaoAtual sessaoAtual)
    {
        _lancamentoService = lancamentoService;
        _pendenciasService = pendenciasService;
        _transferenciaService = transferenciaService;
        _sessaoAtual = sessaoAtual;

        CarregarCommand = new AsyncRelayCommand(CarregarAsync);
        ExcluirLancamentoCommand = new AsyncRelayCommand<Guid>(ExcluirAsync);
        RestaurarLancamentoCommand = new AsyncRelayCommand<Guid>(RestaurarAsync);
        ExcluirTransferenciaCommand = new AsyncRelayCommand<Guid>(ExcluirTransferenciaAsync);
        NovaTransferenciaCommand = new RelayCommand(() => SolicitarNovaTransferencia?.Invoke(this, EventArgs.Empty));
        NovoLancamentoCommand = new RelayCommand(() => SolicitarNovoLancamento?.Invoke(this, EventArgs.Empty));
        EditarLancamentoCommand = new RelayCommand<Guid>(id => SolicitarEdicaoLancamento?.Invoke(this, id));
        GerenciarContasCommand = new RelayCommand(() => SolicitarGerenciarContas?.Invoke(this, EventArgs.Empty));
        LimparFiltrosCommand = new RelayCommand(LimparFiltros);
    }

    public Task InicializarAsync() => CarregarAsync();

    private async Task CarregarAsync()
    {
        MensagemErro = string.Empty;
        try
        {
            var extrato = await _lancamentoService.ObterExtratoConsolidadoAsync(EmpresaId);
            var itens = extrato.Lancamentos;
            _todosLancamentos = itens;
            SaldoAtual = extrato.SaldoTotal;

            // Cards e gráficos mostram receitas e despesas: transferências entre contas ficam de fora
            // (continuam na lista de lançamentos e no saldo de cada conta).
            var doResultado = itens.Where(i => i.ContaNoResultado).ToList();
            var hoje = DateTime.Today;
            TotalEntradasMes = doResultado
                .Where(i => i.Tipo == TipoLancamento.Entrada && i.Data.Year == hoje.Year && i.Data.Month == hoje.Month)
                .Sum(i => i.Valor);
            var saidasDoMes = doResultado
                .Where(i => i.Tipo == TipoLancamento.Saida && i.Data.Year == hoje.Year && i.Data.Month == hoje.Month)
                .ToList();
            TotalSaidasMes = saidasDoMes.Sum(i => i.Valor);

            AtualizarUltimosLancamentos(itens);
            AtualizarFatiasSaidasPorCategoria(saidasDoMes);
            AtualizarEvolucaoMensal(doResultado);
            ExecutarComFiltrosSuspensos(() => AtualizarOpcoesDeFiltro(itens));

            AtualizarPendencias(await _pendenciasService.ObterAsync(EmpresaId, DateTime.Today));
        }
        catch (DomainException ex)
        {
            MensagemErro = ex.Message;
        }
    }

    private void AtualizarPendencias(PendenciasDto pendencias)
    {
        const int maximoMesesListados = 3;
        var frases = new List<string>();

        if (pendencias.LancamentosSemComprovante > 0)
        {
            var quantidade = pendencias.LancamentosSemComprovante;
            frases.Add(quantidade == 1
                ? "1 lançamento sem comprovante anexado"
                : $"{quantidade} lançamentos sem comprovante anexado");
        }

        foreach (var (ano, mes) in pendencias.CompetenciasNaoExportadas.Take(maximoMesesListados))
            frases.Add($"{NomeCompetencia(ano, mes)} ainda não foi exportado para o escritório");

        var restantes = pendencias.CompetenciasNaoExportadas.Count - maximoMesesListados;
        if (restantes > 0)
            frases.Add($"e mais {restantes} {(restantes == 1 ? "mês" : "meses")} sem exportar");

        Pendencias.SubstituirPor(frases);
        TemPendencias = pendencias.TemPendencias;
    }

    private static string NomeCompetencia(int ano, int mes)
    {
        var nome = new DateTime(ano, mes, 1).ToString("MMMM", CultureInfo.CurrentCulture);
        return $"{char.ToUpper(nome[0], CultureInfo.CurrentCulture)}{nome[1..]}/{ano}";
    }

    private void AtualizarOpcoesDeFiltro(IEnumerable<LancamentoResumoDto> itens)
    {
        // Guardados antes do Clear(): ao limpar as opções, o ComboBox perde o item selecionado e
        // devolve null pelo binding, o que resetava o filtro do usuário a cada recarga.
        var contaAnterior = FiltroConta;
        var categoriaAnterior = FiltroCategoria;

        var contas = itens.Select(i => i.Conta).Distinct().OrderBy(c => c).ToList();
        var categorias = itens.Select(i => i.Categoria).Distinct().OrderBy(c => c).ToList();

        ContasParaFiltro.Clear();
        ContasParaFiltro.Add(TodasOpcaoFiltro);
        foreach (var conta in contas)
            ContasParaFiltro.Add(conta);

        CategoriasParaFiltro.Clear();
        CategoriasParaFiltro.Add(TodasOpcaoFiltro);
        foreach (var categoria in categorias)
            CategoriasParaFiltro.Add(categoria);

        FiltroConta = ContasParaFiltro.Contains(contaAnterior) ? contaAnterior : TodasOpcaoFiltro;
        FiltroCategoria = CategoriasParaFiltro.Contains(categoriaAnterior) ? categoriaAnterior : TodasOpcaoFiltro;
    }

    private void ExecutarComFiltrosSuspensos(Action alterarFiltros)
    {
        _filtrosSuspensos = true;
        try
        {
            alterarFiltros();
        }
        finally
        {
            _filtrosSuspensos = false;
        }

        AplicarFiltros();
    }

    private void AplicarFiltros()
    {
        if (_filtrosSuspensos)
            return;

        var dataInicio = FiltroDataInicio is null ? (DateOnly?)null : DateOnly.FromDateTime(FiltroDataInicio.Value);
        var dataFim = FiltroDataFim is null ? (DateOnly?)null : DateOnly.FromDateTime(FiltroDataFim.Value);
        var busca = TextoBusca.Trim();

        LancamentosFiltrados.SubstituirPor(_todosLancamentos.Where(l =>
            (FiltroConta == TodasOpcaoFiltro || l.Conta == FiltroConta) &&
            (FiltroCategoria == TodasOpcaoFiltro || l.Categoria == FiltroCategoria) &&
            (dataInicio is null || l.Data >= dataInicio) &&
            (dataFim is null || l.Data <= dataFim) &&
            (busca.Length == 0 || BuscaTexto.Contem(l.Descricao, busca) || BuscaTexto.Contem(l.Observacoes, busca))));
    }

    private void LimparFiltros() => ExecutarComFiltrosSuspensos(() =>
    {
        FiltroConta = TodasOpcaoFiltro;
        FiltroCategoria = TodasOpcaoFiltro;
        FiltroDataInicio = null;
        FiltroDataFim = null;
        TextoBusca = string.Empty;
    });

    partial void OnFiltroContaChanged(string value) => AplicarFiltros();

    partial void OnFiltroCategoriaChanged(string value) => AplicarFiltros();

    partial void OnFiltroDataInicioChanged(DateTime? value) => AplicarFiltros();

    partial void OnFiltroDataFimChanged(DateTime? value) => AplicarFiltros();

    partial void OnTextoBuscaChanged(string value) => AplicarFiltros();

    private void AtualizarUltimosLancamentos(IEnumerable<LancamentoResumoDto> itens) =>
        UltimosLancamentos.SubstituirPor(itens.Take(5));

    /// <summary>
    /// Monta as fatias do gráfico de pizza de saídas por categoria (mês atual). Mostra as até 7
    /// categorias de maior valor com cores individuais e agrupa o restante numa fatia "Outras
    /// categorias" — regra e paleta validada em REESTRUTURACAO_VISUAL.md, seção 1.3.
    /// </summary>
    private void AtualizarFatiasSaidasPorCategoria(IReadOnlyList<LancamentoResumoDto> saidasDoMes)
    {
        FatiasSaidasPorCategoria.Clear();

        var agrupado = saidasDoMes
            .GroupBy(i => i.Categoria)
            .Select(g => new { Categoria = g.Key, Total = g.Sum(i => i.Valor) })
            .OrderByDescending(g => g.Total)
            .ToList();

        if (agrupado.Count == 0)
            return;

        var itens = agrupado.Take(7).Select(g => (g.Categoria, g.Total)).ToList();
        var restante = agrupado.Skip(7).Sum(g => g.Total);
        if (restante > 0)
            itens.Add(("Outras categorias", restante));

        var totalGeral = itens.Sum(i => i.Item2);
        if (totalGeral <= 0)
            return;

        const double cx = 75, cy = 75, raio = 70;
        var cores = ObterCoresCategorias();
        var anguloAcumulado = 0.0;

        for (var i = 0; i < itens.Count; i++)
        {
            var (categoria, total) = itens[i];
            var anguloInicio = anguloAcumulado;
            var anguloFim = anguloAcumulado + (double)(total / totalGeral) * 360.0;
            anguloAcumulado = anguloFim;

            var cor = categoria == "Outras categorias" ? ObterCorOutras() : cores[i % cores.Count];
            var geometria = itens.Count == 1
                ? (Geometry)new EllipseGeometry(new Point(cx, cy), raio, raio)
                : CriarFatiaPizza(cx, cy, raio, anguloInicio, anguloFim);
            geometria.Freeze();

            FatiasSaidasPorCategoria.Add(new FatiaPizza(categoria, total, cor, geometria));
        }
    }

    private static Geometry CriarFatiaPizza(double cx, double cy, double raio, double anguloInicioGraus, double anguloFimGraus)
    {
        Point ParaPonto(double anguloGraus)
        {
            var rad = (anguloGraus - 90.0) * Math.PI / 180.0;
            return new Point(cx + raio * Math.Cos(rad), cy + raio * Math.Sin(rad));
        }

        var figura = new PathFigure { StartPoint = new Point(cx, cy), IsClosed = true };
        figura.Segments.Add(new LineSegment(ParaPonto(anguloInicioGraus), true));
        figura.Segments.Add(new ArcSegment(
            ParaPonto(anguloFimGraus), new Size(raio, raio), 0,
            isLargeArc: (anguloFimGraus - anguloInicioGraus) > 180.0,
            SweepDirection.Clockwise, true));

        var geometria = new PathGeometry();
        geometria.Figures.Add(figura);
        return geometria;
    }

    private static IReadOnlyList<Brush> ObterCoresCategorias() => new List<Brush>
    {
        (Brush)System.Windows.Application.Current.Resources["Brush.Chart1"],
        (Brush)System.Windows.Application.Current.Resources["Brush.Chart2"],
        (Brush)System.Windows.Application.Current.Resources["Brush.Chart3"],
        (Brush)System.Windows.Application.Current.Resources["Brush.Chart4"],
        (Brush)System.Windows.Application.Current.Resources["Brush.Chart5"],
        (Brush)System.Windows.Application.Current.Resources["Brush.Chart6"],
        (Brush)System.Windows.Application.Current.Resources["Brush.Chart7"],
    };

    private static Brush ObterCorOutras() => (Brush)System.Windows.Application.Current.Resources["Brush.ChartOutras"];

    private void AtualizarEvolucaoMensal(IReadOnlyList<LancamentoResumoDto> itens)
    {
        EvolucaoMensal.Clear();
        if (itens.Count == 0)
            return;

        var primeiroDiaDoMesAtual = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        var meses = Enumerable.Range(0, 6)
            .Select(i => primeiroDiaDoMesAtual.AddMonths(-i))
            .Reverse()
            .ToList();

        // Agrupa uma vez só (em vez de varrer o extrato inteiro duas vezes por mês).
        var totaisPorMesETipo = itens
            .GroupBy(i => (i.Data.Year, i.Data.Month, i.Tipo))
            .ToDictionary(g => g.Key, g => g.Sum(i => i.Valor));

        var porMes = meses
            .Select(mes => new
            {
                Mes = mes,
                Entradas = totaisPorMesETipo.GetValueOrDefault((mes.Year, mes.Month, TipoLancamento.Entrada)),
                Saidas = totaisPorMesETipo.GetValueOrDefault((mes.Year, mes.Month, TipoLancamento.Saida))
            })
            .ToList();

        var maiorValor = porMes.Max(m => Math.Max(m.Entradas, m.Saidas));

        foreach (var m in porMes)
        {
            var percentualEntradas = maiorValor > 0 ? (double)(m.Entradas / maiorValor) * 100.0 : 0.0;
            var percentualSaidas = maiorValor > 0 ? (double)(m.Saidas / maiorValor) * 100.0 : 0.0;
            EvolucaoMensal.Add(new EvolucaoMensalItem(
                m.Mes.ToString("MMM/yy"), m.Entradas, m.Saidas, percentualEntradas, percentualSaidas));
        }
    }

    private static List<DiaSemanaItem> ComputarSemanaAtual()
    {
        var hoje = DateTime.Today;
        var diferencaParaSegunda = ((int)hoje.DayOfWeek + 6) % 7;
        var segunda = hoje.AddDays(-diferencaParaSegunda);
        string[] abreviacoes = { "SEG", "TER", "QUA", "QUI", "SEX" };

        var dias = new List<DiaSemanaItem>();
        for (var i = 0; i < abreviacoes.Length; i++)
        {
            var dia = segunda.AddDays(i);
            dias.Add(new DiaSemanaItem(abreviacoes[i], dia.Day, dia.Date == hoje));
        }

        return dias;
    }

    private async Task ExcluirAsync(Guid lancamentoId)
    {
        try
        {
            await _lancamentoService.ExcluirAsync(lancamentoId);
            await CarregarAsync();
            if (ExibirExcluidos)
                await CarregarExcluidosAsync();
        }
        catch (DomainException ex)
        {
            MensagemErro = ex.Message;
        }
    }

    private async Task ExcluirTransferenciaAsync(Guid transferenciaId)
    {
        try
        {
            await _transferenciaService.ExcluirAsync(transferenciaId);
            await CarregarAsync();
        }
        catch (DomainException ex)
        {
            MensagemErro = ex.Message;
        }
    }

    private async Task RestaurarAsync(Guid lancamentoId)
    {
        try
        {
            await _lancamentoService.RestaurarAsync(lancamentoId);
            await CarregarAsync();
            if (ExibirExcluidos)
                await CarregarExcluidosAsync();
        }
        catch (DomainException ex)
        {
            MensagemErro = ex.Message;
        }
    }

    partial void OnExibirExcluidosChanged(bool value)
    {
        if (value)
            _ = CarregarExcluidosAsync();
        else
            Excluidos.Clear();
    }

    private async Task CarregarExcluidosAsync()
    {
        try
        {
            Excluidos.SubstituirPor(await _lancamentoService.ListarExcluidosAsync(EmpresaId));
        }
        catch (DomainException ex)
        {
            MensagemErro = ex.Message;
        }
    }

    public Task RecarregarAsync() => CarregarAsync();
}
