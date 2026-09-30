using System.Windows;

namespace LivroCaixa.App.Views;

/// <summary>
/// Termo de uso/privacidade. Em modo aceite, DialogResult = true só se o usuário marcar
/// "Li e concordo" e clicar em Aceitar; fechar pelo X equivale a recusar.
/// Em modo leitura (consulta posterior), só exibe o texto.
/// </summary>
public partial class TermoUsoWindow : Window
{
    public TermoUsoWindow(bool modoAceite)
    {
        InitializeComponent();
        ListaSecoes.ItemsSource = TermoDeUso.Secoes;

        if (!modoAceite)
        {
            PainelAceite.Visibility = Visibility.Collapsed;
            BotaoFechar.Visibility = Visibility.Visible;
            TextoIntroducao.Text = "Como o aplicativo trata os dados da sua organização.";
        }
    }

    private void CaixaConcordo_Alterada(object sender, RoutedEventArgs e) =>
        BotaoAceitar.IsEnabled = CaixaConcordo.IsChecked == true;

    private void BotaoAceitar_Click(object sender, RoutedEventArgs e) => DialogResult = true;

    private void BotaoRecusar_Click(object sender, RoutedEventArgs e) => DialogResult = false;

    private void BotaoFechar_Click(object sender, RoutedEventArgs e) => Close();
}
