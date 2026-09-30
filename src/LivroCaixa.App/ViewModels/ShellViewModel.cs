using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LivroCaixa.Application;
using LivroCaixa.Application.Interfaces;
using LivroCaixa.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace LivroCaixa.App.ViewModels;

public partial class ShellViewModel : ObservableObject
{
    private readonly SessaoAtual _sessaoAtual;
    private readonly IServiceScopeFactory _fabricaEscopos;
    private readonly IRegistroLog _registroLog;
    private readonly SegurancaService _segurancaService;

    public ExtratoViewModel Extrato { get; }

    public RelatorioViewModel Relatorio { get; }

    public Guid EmpresaId => _sessaoAtual.EmpresaId;

    public Guid UsuarioId => _sessaoAtual.UsuarioId;

    public string NomeUsuario => _sessaoAtual.NomeUsuario;

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
    private PaginaShell paginaSelecionada = PaginaShell.Dashboard;

    [ObservableProperty]
    private bool sidebarExpandida = true;

    public event EventHandler? SolicitarAbrirContas;

    public event EventHandler? SolicitarAbrirPerfil;

    public event EventHandler? SolicitarSair;

    public event EventHandler? SolicitarAbrirCategorias;

    public event EventHandler? SolicitarAbrirModelos;

    public event EventHandler? SolicitarAbrirExportar;

    public event EventHandler? SolicitarAbrirArmazenamento;

    public event EventHandler? SolicitarAbrirConfiguracoes;

    public event EventHandler? SolicitarAbrirSobre;

    public event EventHandler? SolicitarAbrirFaq;

    public IRelayCommand IrParaDashboardCommand { get; }

    public IRelayCommand IrParaLancamentosCommand { get; }

    public IRelayCommand IrParaRelatoriosCommand { get; }

    public IRelayCommand AbrirContasCommand { get; }

    public IRelayCommand AbrirCategoriasCommand { get; }

    public IRelayCommand AbrirModelosCommand { get; }

    public IRelayCommand AbrirExportarCommand { get; }

    public IRelayCommand AbrirArmazenamentoCommand { get; }

    public IRelayCommand AbrirConfiguracoesCommand { get; }

    public IRelayCommand AbrirSobreCommand { get; }

    public IRelayCommand AbrirFaqCommand { get; }

    public IRelayCommand AlternarSidebarCommand { get; }

    public IRelayCommand AbrirPerfilCommand { get; }

    public IRelayCommand SairCommand { get; }

    public ShellViewModel(
        ExtratoViewModel extrato,
        RelatorioViewModel relatorio,
        SegurancaService segurancaService,
        IServiceScopeFactory fabricaEscopos,
        IRegistroLog registroLog,
        SessaoAtual sessaoAtual)
    {
        Extrato = extrato;
        Relatorio = relatorio;
        _fabricaEscopos = fabricaEscopos;
        _registroLog = registroLog;
        _segurancaService = segurancaService;
        _sessaoAtual = sessaoAtual;

        IrParaDashboardCommand = new RelayCommand(() => PaginaSelecionada = PaginaShell.Dashboard);
        IrParaLancamentosCommand = new RelayCommand(() => PaginaSelecionada = PaginaShell.Lancamentos);
        IrParaRelatoriosCommand = new RelayCommand(() => PaginaSelecionada = PaginaShell.Relatorios);
        AbrirContasCommand = new RelayCommand(() => SolicitarAbrirContas?.Invoke(this, EventArgs.Empty));
        AbrirCategoriasCommand = new RelayCommand(() => SolicitarAbrirCategorias?.Invoke(this, EventArgs.Empty));
        AbrirModelosCommand = new RelayCommand(() => SolicitarAbrirModelos?.Invoke(this, EventArgs.Empty));
        AbrirExportarCommand = new RelayCommand(() => SolicitarAbrirExportar?.Invoke(this, EventArgs.Empty));
        AbrirArmazenamentoCommand = new RelayCommand(() => SolicitarAbrirArmazenamento?.Invoke(this, EventArgs.Empty));
        AbrirConfiguracoesCommand = new RelayCommand(() => SolicitarAbrirConfiguracoes?.Invoke(this, EventArgs.Empty));
        AbrirSobreCommand = new RelayCommand(() => SolicitarAbrirSobre?.Invoke(this, EventArgs.Empty));
        AbrirFaqCommand = new RelayCommand(() => SolicitarAbrirFaq?.Invoke(this, EventArgs.Empty));
        AlternarSidebarCommand = new RelayCommand(() => SidebarExpandida = !SidebarExpandida);
        AbrirPerfilCommand = new RelayCommand(() => SolicitarAbrirPerfil?.Invoke(this, EventArgs.Empty));
        SairCommand = new RelayCommand(() => SolicitarSair?.Invoke(this, EventArgs.Empty));
    }

    public Task<int> ObterMinutosBloqueioInatividadeAsync() => _segurancaService.ObterMinutosBloqueioInatividadeAsync();

    public async Task InicializarAsync()
    {
        await Extrato.InicializarAsync();
        await Relatorio.InicializarAsync();

        // Melhor esforço: se o backup automático estiver ativo e o prazo já tiver vencido,
        // roda em segundo plano sem atrasar a abertura do Dashboard. Falha aqui não deve
        // impedir o uso do app — só não atualiza a data do último backup.
        _ = ExecutarBackupAutomaticoEmSegundoPlanoAsync();
    }

    private async Task ExecutarBackupAutomaticoEmSegundoPlanoAsync()
    {
        try
        {
            // Escopo (e DbContext) próprio: o backup termina em segundo plano enquanto o usuário
            // já usa as telas, e o DbContext compartilhado pela janela principal não suporta duas
            // operações ao mesmo tempo — nem deve salvar alterações pendentes de outra tela.
            using var escopo = _fabricaEscopos.CreateScope();
            var backupService = escopo.ServiceProvider.GetRequiredService<BackupService>();
            await backupService.ExecutarBackupAutomaticoSeNecessarioAsync(EmpresaId);
        }
        catch (Exception ex)
        {
            // Melhor esforço — sem UI de erro aqui; o usuário pode conferir a data do
            // último backup na tela de Configurações e tentar manualmente se precisar.
            // Mas fica no log: sem isso, um backup que falha toda vez passaria despercebido.
            _registroLog.RegistrarErro("Falha no backup automático", ex);
        }
    }
}
