using System.Windows;

namespace LivroCaixa.App.Views;

public partial class SobreWindow : Window
{
    public SobreWindow()
    {
        InitializeComponent();

        TextoNome.Text = DadosDoEscritorio.Nome;
        TextoProposta.Text = DadosDoEscritorio.PropostaDeValor;
        TextoEmail.Text = DadosDoEscritorio.EmailContabil;

        DefinirOuOcultarLinha(LinhaTelefone, TextoTelefone, DadosDoEscritorio.Telefone);
        DefinirOuOcultarLinha(LinhaEndereco, TextoEndereco, DadosDoEscritorio.Endereco);

        TextoRazaoSocial.Text = DadosDoEscritorio.RazaoSocial;
        DefinirOuOcultarLinha(LinhaCnpj, TextoCnpj, DadosDoEscritorio.Cnpj is null ? null : $"CNPJ: {DadosDoEscritorio.Cnpj}");
        SeparadorCnpj.Visibility = LinhaCnpj.Visibility;
    }

    private static void DefinirOuOcultarLinha(UIElement linha, System.Windows.Controls.TextBlock textBlock, string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
        {
            linha.Visibility = Visibility.Collapsed;
            return;
        }

        textBlock.Text = texto;
        linha.Visibility = Visibility.Visible;
    }

    private void BotaoFechar_Click(object sender, RoutedEventArgs e) => Close();
}
