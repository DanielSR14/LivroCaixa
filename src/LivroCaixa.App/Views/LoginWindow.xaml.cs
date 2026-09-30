using System.Windows;
using LivroCaixa.App.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace LivroCaixa.App.Views;

public partial class LoginWindow : Window
{
    private readonly LoginViewModel _viewModel;

    public LoginWindow(LoginViewModel viewModel)
    {
        InitializeComponent();

        _viewModel = viewModel;
        DataContext = _viewModel;
        _viewModel.LoginConcluido += (_, _) =>
        {
            DialogResult = true;
            Close();
        };
    }

    private void CampoSenha_PasswordChanged(object sender, RoutedEventArgs e) =>
        _viewModel.Senha = CampoSenha.Password;

    private void LinkEsqueciSenha_Click(object sender, RoutedEventArgs e)
    {
        var janela = App.Services.GetRequiredService<RecuperarAcessoWindow>();
        janela.Owner = this;
        janela.PreencherNomeUsuario(_viewModel.NomeUsuario);

        if (janela.ShowDialog() == true)
        {
            CampoSenha.Clear();
            CampoSenha.Focus();
            _viewModel.MensagemErro = string.Empty;
            _viewModel.MensagemInformativa = "Senha alterada. Entre com a nova senha.";
        }
    }
}
