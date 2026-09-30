using System.Windows;
using LivroCaixa.App.ViewModels;

namespace LivroCaixa.App.Views;

public partial class RecuperarAcessoWindow : Window
{
    private readonly RecuperarAcessoViewModel _viewModel;

    public RecuperarAcessoWindow(RecuperarAcessoViewModel viewModel)
    {
        InitializeComponent();

        _viewModel = viewModel;
        DataContext = _viewModel;
        _viewModel.AcessoRecuperado += (_, novoCodigo) =>
        {
            // O código usado deixou de valer: o novo precisa ser guardado antes de voltar ao login.
            new CodigoRecuperacaoWindow(novoCodigo,
                "Senha alterada. O código que você usou não vale mais — este é o seu novo código de recuperação.")
            { Owner = this }.ShowDialog();

            DialogResult = true;
        };
    }

    /// <summary>Já traz preenchido o nome que estava digitado na tela de login.</summary>
    public void PreencherNomeUsuario(string nomeUsuario) => _viewModel.NomeUsuario = nomeUsuario;

    private void CampoNovaSenha_PasswordChanged(object sender, RoutedEventArgs e) =>
        _viewModel.NovaSenha = CampoNovaSenha.Password;

    private void CampoConfirmarSenha_PasswordChanged(object sender, RoutedEventArgs e) =>
        _viewModel.ConfirmarNovaSenha = CampoConfirmarSenha.Password;
}
