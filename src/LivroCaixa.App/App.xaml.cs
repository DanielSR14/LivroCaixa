using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using LivroCaixa.App.ViewModels;
using LivroCaixa.App.Views;
using LivroCaixa.Application;
using LivroCaixa.Application.Interfaces;
using LivroCaixa.Application.Services;
using LivroCaixa.Infrastructure;
using LivroCaixa.Infrastructure.Database;
using LivroCaixa.Infrastructure.Logs;
using Microsoft.Extensions.DependencyInjection;
using Velopack;

namespace LivroCaixa.App;

public partial class App : System.Windows.Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    /// <summary>
    /// Instância própria, criada antes da DI: erros durante a inicialização (ex: migration que
    /// falha) precisam ser registrados mesmo quando o ServiceProvider nem chegou a existir.
    /// </summary>
    private static readonly RegistroLogArquivo RegistroLog = new();

    /// <summary>Versão do instalador (passada ao dotnet publish por installer/gerar-instalador.bat).</summary>
    public static string Versao { get; } =
        (typeof(App).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "?")
            .Split('+')[0];

    /// <summary>
    /// Até a janela principal abrir, um erro inesperado é fatal (encerra o app): sem isso, com
    /// ShutdownMode.OnExplicitShutdown, o processo ficaria rodando sem nenhuma janela.
    /// </summary>
    private bool _janelaPrincipalAberta;

    private bool _exibindoMensagemErro;

    // Campo estático: o mutex precisa viver o processo inteiro (se for coletado, a trava some).
    private static Mutex? _instanciaUnica;
    private static SplashScreen? _splash;
    private static Task<bool> _existeEmpresaCadastrada = null!;

    /// <summary>
    /// Substitui o Main gerado automaticamente pelo WPF a partir de App.xaml, porque o
    /// Velopack precisa rodar antes de absolutamente qualquer outra coisa (ele trata
    /// eventos de instalação/atualização/desinstalação na primeira execução pós-instalador).
    /// </summary>
    [STAThread]
    private static void Main(string[] args)
    {
        VelopackApp.Build().Run();

        // Um segundo clique no atalho enquanto o app ainda abre (ou já aberto) não pode criar outra
        // cópia disputando o mesmo banco: traz a janela existente para a frente e sai.
        _instanciaUnica = new Mutex(true, @"Local\LivroCaixa", out var primeiraInstancia);
        if (!primeiraInstancia)
        {
            InstanciaUnica.AtivarJanelaDaInstanciaExistente();
            return;
        }

        // Imagem nativa (sem XAML), na tela antes de qualquer outra coisa carregar; some com fade
        // quando a primeira janela de verdade termina de desenhar (ver MostrarPrimeiraJanela).
        _splash = new SplashScreen(typeof(App).Assembly, "Assets/splash.png");
        _splash.Show(autoClose: false, topMost: false);

        // A decisão login × cadastro sai de uma consulta SQLite crua, em paralelo com o carregamento
        // do tema; as migrations (lentas) ficam para depois da tela aparecer (ver InicializacaoBanco).
        _existeEmpresaCadastrada = Task.Run(() =>
            VerificacaoRapidaBanco.ExisteEmpresaCadastrada(CaminhosAplicacao.CaminhoBancoDados));

        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            RegistroLog.RegistrarErro("Erro não tratado fora da interface (app encerrado)", (Exception)e.ExceptionObject);
        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            RegistroLog.RegistrarErro("Erro em tarefa em segundo plano", e.Exception);
            e.SetObserved();
        };

        var app = new App();
        app.DispatcherUnhandledException += app.AoOcorrerErroNaoTratado;
        app.InitializeComponent();
        app.Run();
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        Task.Run(() => RegistroLog.RemoverLogsAntigos(DateTime.Now));
        RegistroLog.RegistrarInformacao($"App iniciado — versão {Versao}, {Environment.OSVersion}");

        var culturaPtBr = CultureInfo.GetCultureInfo("pt-BR");
        Thread.CurrentThread.CurrentCulture = culturaPtBr;
        Thread.CurrentThread.CurrentUICulture = culturaPtBr;
        CultureInfo.DefaultThreadCurrentCulture = culturaPtBr;
        CultureInfo.DefaultThreadCurrentUICulture = culturaPtBr;
        FrameworkElement.LanguageProperty.OverrideMetadata(
            typeof(FrameworkElement),
            new FrameworkPropertyMetadata(System.Windows.Markup.XmlLanguage.GetLanguage(culturaPtBr.IetfLanguageTag)));

        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        var servicos = new ServiceCollection();
        servicos.AdicionarInfraestrutura();
        servicos.AdicionarApplication();
        RegistrarTelas(servicos);
        Services = servicos.BuildServiceProvider();

        // Aguardada antes de iniciar as migrations, para as duas não disputarem o arquivo do banco.
        var existeEmpresa = _existeEmpresaCadastrada.GetAwaiter().GetResult();
        Services.GetRequiredService<InicializacaoBanco>().Iniciar(Services);

        var entrou = existeEmpresa
            ? MostrarPrimeiraJanela(Services.GetRequiredService<LoginWindow>())
            // LGPD: o termo é aceito antes de o cadastro coletar qualquer dado da organização.
            : MostrarPrimeiraJanela(new TermoUsoWindow(modoAceite: true))
              && Services.GetRequiredService<CadastroInicialWindow>().ShowDialog() == true;

        if (!entrou || !ConcluirEntrada())
        {
            Shutdown();
            return;
        }

        AbrirJanelaPrincipal();
    }

    /// <summary>
    /// A splash só sai quando a janela já está desenhada: fechar antes de ShowDialog deixaria
    /// um intervalo sem nada na tela enquanto o XAML da janela carrega.
    /// </summary>
    private static bool MostrarPrimeiraJanela(Window janela)
    {
        janela.ContentRendered += (_, _) => FecharSplash();
        return janela.ShowDialog() == true;
    }

    private static void FecharSplash()
    {
        _splash?.Close(TimeSpan.FromMilliseconds(200));
        _splash = null;
    }

    /// <summary>
    /// Fecha a janela principal atual, limpa a sessão e mostra a tela de login de novo — usado
    /// pelo botão "Sair" e pela tela de bloqueio. Se o login for cancelado, encerra o app (mesmo
    /// comportamento do cancelamento no <see cref="OnStartup"/>).
    /// </summary>
    public void ReiniciarParaLogin(Window janelaAtual)
    {
        ShutdownMode = ShutdownMode.OnExplicitShutdown;
        _janelaPrincipalAberta = false;
        janelaAtual.Close();

        if (Services.GetRequiredService<LoginWindow>().ShowDialog() != true || !ConcluirEntrada())
        {
            Shutdown();
            return;
        }

        AbrirJanelaPrincipal();
    }

    private void AbrirJanelaPrincipal()
    {
        ShutdownMode = ShutdownMode.OnMainWindowClose;
        var janelaPrincipal = Services.GetRequiredService<MainWindow>();
        MainWindow = janelaPrincipal;
        janelaPrincipal.Show();
        _janelaPrincipalAberta = true;
    }

    /// <summary>
    /// Depois de autenticar e antes da janela principal: pede o aceite do termo se ele estiver
    /// pendente (versão nova, ou cliente que já usava o app antes de o termo existir) e mostra o
    /// código de recuperação se o usuário ainda não tiver um. Retorna false se o termo for recusado.
    /// </summary>
    private static bool ConcluirEntrada()
    {
        var usuarioId = Services.GetRequiredService<SessaoAtual>().UsuarioId;
        var seguranca = Services.GetRequiredService<SegurancaService>();

        if (seguranca.PrecisaAceitarTermoAsync(TermoDeUso.VersaoAtual).GetAwaiter().GetResult())
        {
            if (new TermoUsoWindow(modoAceite: true).ShowDialog() != true)
                return false;

            seguranca.RegistrarAceiteTermoAsync(TermoDeUso.VersaoAtual).GetAwaiter().GetResult();
        }

        if (seguranca.PrecisaGerarCodigoRecuperacaoAsync(usuarioId).GetAwaiter().GetResult())
        {
            // Só grava depois da confirmação "guardei": se o app fechar antes, o código é oferecido de novo.
            var codigo = CodigoRecuperacao.Gerar();
            var guardou = new CodigoRecuperacaoWindow(codigo,
                "Se um dia você esquecer a senha, este código permite criar uma nova, na tela de entrada " +
                "(\"Esqueci minha senha\"). Sem a senha e sem o código, não há como recuperar o acesso aos dados.")
                .ShowDialog() == true;

            if (guardou)
                seguranca.DefinirCodigoRecuperacaoAsync(usuarioId, codigo).GetAwaiter().GetResult();
        }

        return true;
    }

    /// <summary>
    /// Última linha de defesa para erros que não são de regra de negócio (DomainException já é
    /// tratada nas ViewModels com mensagem própria): banco, disco, arquivo travado, bug. Registra no
    /// log e mostra uma mensagem amigável em vez de o app simplesmente fechar sem explicação.
    /// </summary>
    private void AoOcorrerErroNaoTratado(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        e.Handled = true;
        RegistroLog.RegistrarErro("Erro não tratado na interface", e.Exception);
        FecharSplash();

        if (!_janelaPrincipalAberta)
        {
            MostrarMensagemErro(
                "O Livro Caixa não conseguiu abrir por causa de um erro inesperado.\n\n" +
                "Tente abrir o programa novamente. Se o problema continuar, envie o registro do erro para o escritório.");
            Shutdown(1);
            return;
        }

        DescartarAlteracoesPendentes();
        MostrarMensagemErro(
            "Ocorreu um erro inesperado e a última ação pode não ter sido concluída.\n\n" +
            "O programa continua funcionando. Se o problema se repetir, envie o registro do erro para o escritório.");
    }

    private static void DescartarAlteracoesPendentes()
    {
        try
        {
            // Resolvido do provedor raiz, é o mesmo DbContext usado pela janela principal.
            Services?.GetService<IUnitOfWork>()?.DescartarAlteracoesPendentes();
        }
        catch (Exception ex)
        {
            RegistroLog.RegistrarErro("Falha ao descartar alterações pendentes após erro", ex);
        }
    }

    private void MostrarMensagemErro(string mensagem)
    {
        // Um erro repetido (ex: num evento disparado várias vezes) não pode empilhar dezenas de caixas.
        if (_exibindoMensagemErro)
            return;

        _exibindoMensagemErro = true;
        try
        {
            var resposta = MessageBox.Show(
                mensagem + "\n\nDeseja abrir a pasta com o registro do erro agora?",
                "Livro Caixa — erro inesperado",
                MessageBoxButton.YesNo,
                MessageBoxImage.Error);

            if (resposta == MessageBoxResult.Yes)
                Process.Start(new ProcessStartInfo("explorer.exe", $"\"{RegistroLog.DiretorioLogs}\"") { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            RegistroLog.RegistrarErro("Falha ao exibir a mensagem de erro", ex);
        }
        finally
        {
            _exibindoMensagemErro = false;
        }
    }

    private static void RegistrarTelas(IServiceCollection servicos)
    {
        servicos.AddSingleton<InicializacaoBanco>();
        servicos.AddTransient<CadastroInicialViewModel>();
        servicos.AddTransient<CadastroInicialWindow>();
        servicos.AddTransient<LoginViewModel>();
        servicos.AddTransient<LoginWindow>();
        servicos.AddTransient<ExtratoViewModel>();
        servicos.AddTransient<RelatorioViewModel>();
        servicos.AddTransient<ShellViewModel>();
        servicos.AddTransient<MainWindow>();
        servicos.AddTransient<LancamentoFormViewModelFactory>();
        servicos.AddTransient<ExportacaoViewModelFactory>();
        servicos.AddTransient<LancamentoDetalheViewModelFactory>();
        servicos.AddTransient<ContasViewModelFactory>();
        servicos.AddTransient<NovaContaViewModelFactory>();
        servicos.AddTransient<ArmazenamentoViewModelFactory>();
        servicos.AddTransient<CategoriasViewModelFactory>();
        servicos.AddTransient<NovaCategoriaViewModelFactory>();
        servicos.AddTransient<ConfiguracoesViewModelFactory>();
        servicos.AddTransient<PerfilViewModel>();
        servicos.AddTransient<PerfilWindow>();
        servicos.AddTransient<RecuperarAcessoViewModel>();
        servicos.AddTransient<RecuperarAcessoWindow>();
        servicos.AddTransient<TelaBloqueioViewModel>();
        servicos.AddTransient<TelaBloqueioWindow>();
        servicos.AddTransient<ModelosViewModelFactory>();
        servicos.AddTransient<ModeloFormViewModelFactory>();
        servicos.AddTransient<TransferenciaViewModelFactory>();
    }
}
