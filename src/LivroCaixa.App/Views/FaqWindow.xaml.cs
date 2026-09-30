using System.Windows;

namespace LivroCaixa.App.Views;

public partial class FaqWindow : Window
{
    public FaqWindow()
    {
        InitializeComponent();
    }

    private void BotaoFechar_Click(object sender, RoutedEventArgs e) => Close();
}
