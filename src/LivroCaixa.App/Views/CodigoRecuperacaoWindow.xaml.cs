using System.ComponentModel;
using System.IO;
using System.Windows;
using Microsoft.Win32;

namespace LivroCaixa.App.Views;

/// <summary>
/// Mostra o código de recuperação recém-gerado — a única vez em que ele aparece em texto.
/// Só fecha depois de o usuário confirmar que guardou o código (inclusive pelo X), porque
/// perder o código e a senha juntos significa perder o acesso aos dados.
/// </summary>
public partial class CodigoRecuperacaoWindow : Window
{
    private readonly string _codigo;

    public CodigoRecuperacaoWindow(string codigo, string explicacao)
    {
        InitializeComponent();
        _codigo = codigo;
        TextoCodigo.Text = codigo;
        TextoExplicacao.Text = explicacao;
        Closing += CodigoRecuperacaoWindow_Closing;
    }

    private void CodigoRecuperacaoWindow_Closing(object? sender, CancelEventArgs e)
    {
        if (CaixaGuardei.IsChecked == true)
            return;

        e.Cancel = true;
        MostrarStatus("Confirme que guardou o código antes de continuar.");
    }

    private void CaixaGuardei_Alterada(object sender, RoutedEventArgs e) =>
        BotaoContinuar.IsEnabled = CaixaGuardei.IsChecked == true;

    private void BotaoContinuar_Click(object sender, RoutedEventArgs e) => DialogResult = true;

    private void BotaoCopiar_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Clipboard.SetText(_codigo);
            MostrarStatus("Código copiado.");
        }
        catch (Exception)
        {
            // A área de transferência pode estar travada por outro programa; não é motivo para erro.
            MostrarStatus("Não foi possível copiar agora. Anote o código manualmente.");
        }
    }

    private void BotaoSalvar_Click(object sender, RoutedEventArgs e)
    {
        var dialogo = new SaveFileDialog
        {
            Title = "Salvar código de recuperação",
            FileName = "Codigo de recuperacao - Livro Caixa.txt",
            Filter = "Arquivo de texto (*.txt)|*.txt"
        };

        if (dialogo.ShowDialog(this) != true)
            return;

        try
        {
            File.WriteAllText(dialogo.FileName,
                $"Código de recuperação do Livro Caixa: {_codigo}{Environment.NewLine}" +
                $"Gerado em {DateTime.Now:dd/MM/yyyy HH:mm}. Use em \"Esqueci minha senha\", na tela de entrada.{Environment.NewLine}");
            MostrarStatus($"Salvo em {dialogo.FileName}");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            MostrarStatus("Não foi possível salvar nesse local. Escolha outra pasta.");
        }
    }

    private void MostrarStatus(string mensagem)
    {
        TextoStatus.Text = mensagem;
        TextoStatus.Visibility = Visibility.Visible;
    }
}
