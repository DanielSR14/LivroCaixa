using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using LivroCaixa.App.ViewModels;
using LivroCaixa.App.Views;
using Microsoft.Extensions.DependencyInjection;

namespace LivroCaixa.App;

public partial class MainWindow : Window
{
    private readonly ShellViewModel _viewModel;
    private readonly DashboardPageView _paginaDashboard;
    private readonly LancamentosPageView _paginaLancamentos;
    private readonly RelatoriosPageView _paginaRelatorios;
    private readonly MonitorInatividade _monitorInatividade;

    public MainWindow(ShellViewModel viewModel)
    {
        InitializeComponent();

        _viewModel = viewModel;
        DataContext = _viewModel;

        _paginaDashboard = new DashboardPageView(_viewModel.Extrato);
        _paginaLancamentos = new LancamentosPageView(_viewModel.Extrato);
        _paginaRelatorios = new RelatoriosPageView(_viewModel.Relatorio);
        PainelConteudo.Children.Add(_paginaDashboard);
        PainelConteudo.Children.Add(_paginaLancamentos);
        PainelConteudo.Children.Add(_paginaRelatorios);
        AtualizarPaginaVisivel();

        _viewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(ShellViewModel.PaginaSelecionada))
                AtualizarPaginaVisivel();
        };

        _viewModel.SolicitarAbrirContas += async (_, _) => await AbrirContasAsync();
        _viewModel.SolicitarAbrirCategorias += (_, _) => AbrirCategorias();
        _viewModel.SolicitarAbrirModelos += (_, _) => AbrirModelos();
        _viewModel.SolicitarAbrirExportar += (_, _) => AbrirExportar();
        _viewModel.SolicitarAbrirArmazenamento += (_, _) => AbrirArmazenamento();
        _viewModel.SolicitarAbrirConfiguracoes += async (_, _) => await AbrirConfiguracoesAsync();
        _viewModel.SolicitarAbrirSobre += (_, _) => AbrirSobre();
        _viewModel.SolicitarAbrirFaq += (_, _) => AbrirFaq();
        _viewModel.SolicitarAbrirPerfil += (_, _) => AbrirPerfil();
        _viewModel.SolicitarSair += (_, _) => Sair();

        _monitorInatividade = new MonitorInatividade(BloquearPorInatividade);
        Loaded += async (_, _) =>
        {
            await _viewModel.InicializarAsync();
            await AtualizarTempoBloqueioAsync();
            _monitorInatividade.Iniciar();
        };
        Closed += (_, _) => _monitorInatividade.Parar();
    }

    private async Task AtualizarTempoBloqueioAsync() =>
        _monitorInatividade.MinutosLimite = await _viewModel.ObterMinutosBloqueioInatividadeAsync();

    /// <summary>
    /// Abre a tela de bloqueio por cima da janela ativa (inclusive de um diálogo aberto, que continua
    /// intacto por baixo, sem perder o que estava sendo digitado) e borra o conteúdo das demais.
    /// </summary>
    private void BloquearPorInatividade()
    {
        var janelasVisiveis = System.Windows.Application.Current.Windows.OfType<Window>().Where(j => j.IsVisible).ToList();
        foreach (var janela in janelasVisiveis)
        {
            if (janela.Content is UIElement conteudo)
                conteudo.Effect = new BlurEffect { Radius = 24 };
        }

        var telaBloqueio = App.Services.GetRequiredService<TelaBloqueioWindow>();
        // Windows lista as janelas na ordem de abertura: a última visível é o diálogo modal mais recente.
        telaBloqueio.Owner = janelasVisiveis.LastOrDefault() ?? this;
        telaBloqueio.ShowDialog();

        foreach (var janela in janelasVisiveis)
        {
            if (janela.Content is UIElement conteudo)
                conteudo.Effect = null;
        }

        if (!telaBloqueio.SairSolicitado)
            return;

        foreach (var dialogo in janelasVisiveis.Where(j => j != this))
            dialogo.Close();

        // Adiado para depois que os laços modais dos diálogos fechados terminarem de desempilhar.
        Dispatcher.BeginInvoke(() => ((App)System.Windows.Application.Current).ReiniciarParaLogin(this));
    }

    private void AtualizarPaginaVisivel()
    {
        _paginaDashboard.Visibility = _viewModel.PaginaSelecionada == PaginaShell.Dashboard ? Visibility.Visible : Visibility.Collapsed;
        _paginaLancamentos.Visibility = _viewModel.PaginaSelecionada == PaginaShell.Lancamentos ? Visibility.Visible : Visibility.Collapsed;
        _paginaRelatorios.Visibility = _viewModel.PaginaSelecionada == PaginaShell.Relatorios ? Visibility.Visible : Visibility.Collapsed;
    }

    private void BotaoAlternarSidebar_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.AlternarSidebarCommand.Execute(null);

        var larguraDestino = _viewModel.SidebarExpandida ? 240 : 0;
        var animacao = new DoubleAnimation(SidebarBorder.Width, larguraDestino, TimeSpan.FromMilliseconds(200))
        {
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
        };
        SidebarBorder.BeginAnimation(WidthProperty, animacao);
    }

    private async Task AbrirContasAsync()
    {
        var factory = App.Services.GetRequiredService<ContasViewModelFactory>();
        var contasViewModel = factory.Criar(_viewModel.EmpresaId);

        var janela = new ContasWindow(contasViewModel, _viewModel.EmpresaId) { Owner = this };
        janela.ShowDialog();

        // Uma nova conta pode ter sido criada — recarrega pra refletir no saldo total e
        // nas contas disponíveis da próxima vez que o formulário de lançamento abrir.
        await _viewModel.Extrato.RecarregarAsync();
    }

    private void AbrirExportar()
    {
        var factory = App.Services.GetRequiredService<ExportacaoViewModelFactory>();
        var exportacaoViewModel = factory.Criar(_viewModel.EmpresaId);

        var janela = new ExportacaoWindow(exportacaoViewModel) { Owner = this };
        janela.ShowDialog();
    }

    private void AbrirCategorias()
    {
        var factory = App.Services.GetRequiredService<CategoriasViewModelFactory>();
        var categoriasViewModel = factory.Criar(_viewModel.EmpresaId);

        var janela = new CategoriasWindow(categoriasViewModel, _viewModel.EmpresaId) { Owner = this };
        janela.ShowDialog();
    }

    private void AbrirModelos()
    {
        var modelosViewModel = App.Services.GetRequiredService<ModelosViewModelFactory>().Criar(_viewModel.EmpresaId);
        new ModelosWindow(modelosViewModel, _viewModel.EmpresaId) { Owner = this }.ShowDialog();
    }

    private void AbrirArmazenamento()
    {
        var factory = App.Services.GetRequiredService<ArmazenamentoViewModelFactory>();
        var armazenamentoViewModel = factory.Criar(_viewModel.EmpresaId);

        var janela = new ArmazenamentoWindow(armazenamentoViewModel) { Owner = this };
        janela.ShowDialog();
    }

    private async Task AbrirConfiguracoesAsync()
    {
        var factory = App.Services.GetRequiredService<ConfiguracoesViewModelFactory>();
        var configuracoesViewModel = factory.Criar(_viewModel.EmpresaId, _viewModel.UsuarioId);

        var janela = new ConfiguracoesWindow(configuracoesViewModel) { Owner = this };
        janela.ShowDialog();

        await AtualizarTempoBloqueioAsync();
    }

    private void AbrirSobre()
    {
        var janela = new SobreWindow { Owner = this };
        janela.ShowDialog();
    }

    private void AbrirFaq()
    {
        var janela = new FaqWindow { Owner = this };
        janela.ShowDialog();
    }

    private void AbrirPerfil()
    {
        var janela = App.Services.GetRequiredService<PerfilWindow>();
        janela.Owner = this;
        janela.ShowDialog();
    }

    private void Sair()
    {
        var confirmar = MessageBox.Show(
            "Deseja sair da sua conta?",
            "Sair",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (confirmar != MessageBoxResult.Yes)
            return;

        ((App)System.Windows.Application.Current).ReiniciarParaLogin(this);
    }
}
