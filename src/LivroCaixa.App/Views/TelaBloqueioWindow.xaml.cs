using System.ComponentModel;
using System.Windows;
using LivroCaixa.App.ViewModels;

namespace LivroCaixa.App.Views;

/// <summary>
/// Tela modal de bloqueio por inatividade. Não fecha pelo X/Alt+F4: só desbloqueando com a
/// senha ou escolhendo sair da conta (<see cref="SairSolicitado"/>).
/// </summary>
public partial class TelaBloqueioWindow : Window
{
    private readonly TelaBloqueioViewModel _viewModel;
    private bool _podeFechar;

    public bool SairSolicitado { get; private set; }

    public TelaBloqueioWindow(TelaBloqueioViewModel viewModel)
    {
        InitializeComponent();

        _viewModel = viewModel;
        DataContext = _viewModel;
        _viewModel.Desbloqueado += (_, _) => Fechar(sair: false);
        _viewModel.SairSolicitado += (_, _) => Fechar(sair: true);

        Closing += (_, e) => e.Cancel = !_podeFechar;
        Loaded += (_, _) => CampoSenha.Focus();
    }

    private void Fechar(bool sair)
    {
        SairSolicitado = sair;
        _podeFechar = true;
        Close();
    }

    private void CampoSenha_PasswordChanged(object sender, RoutedEventArgs e) =>
        _viewModel.Senha = CampoSenha.Password;
}
