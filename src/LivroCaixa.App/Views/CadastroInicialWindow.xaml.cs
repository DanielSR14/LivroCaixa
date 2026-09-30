using System.Text;
using System.Windows;
using System.Windows.Controls;
using LivroCaixa.App.ViewModels;

namespace LivroCaixa.App.Views;

public partial class CadastroInicialWindow : Window
{
    private readonly CadastroInicialViewModel _viewModel;
    private bool _formatandoCnpj;

    public CadastroInicialWindow(CadastroInicialViewModel viewModel)
    {
        InitializeComponent();

        _viewModel = viewModel;
        DataContext = _viewModel;
        _viewModel.CadastroConcluido += (_, _) =>
        {
            DialogResult = true;
            Close();
        };
    }

    private void CampoSenha_PasswordChanged(object sender, RoutedEventArgs e) =>
        _viewModel.Senha = CampoSenha.Password;

    private void CampoConfirmarSenha_PasswordChanged(object sender, RoutedEventArgs e) =>
        _viewModel.ConfirmarSenha = CampoConfirmarSenha.Password;

    private void CampoCnpj_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_formatandoCnpj)
            return;

        var textoFormatado = FormatarCnpj(CampoCnpj.Text);
        if (textoFormatado == CampoCnpj.Text)
            return;

        _formatandoCnpj = true;
        CampoCnpj.Text = textoFormatado;
        CampoCnpj.CaretIndex = CampoCnpj.Text.Length;
        _formatandoCnpj = false;
    }

    private static string FormatarCnpj(string texto)
    {
        var digitos = new string(texto.Where(char.IsDigit).ToArray());
        if (digitos.Length > 14)
            digitos = digitos[..14];

        var formatado = new StringBuilder();
        for (var i = 0; i < digitos.Length; i++)
        {
            if (i is 2 or 5)
                formatado.Append('.');
            else if (i == 8)
                formatado.Append('/');
            else if (i == 12)
                formatado.Append('-');

            formatado.Append(digitos[i]);
        }

        return formatado.ToString();
    }
}
