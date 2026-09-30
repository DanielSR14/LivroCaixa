using System;
using System.Windows.Controls;
using System.Windows.Threading;
using LivroCaixa.App.ViewModels;

namespace LivroCaixa.App.Views;

public partial class DashboardPageView : UserControl
{
    private readonly DispatcherTimer _timerRelogio;

    public DashboardPageView(ExtratoViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        // O relógio mostra só HH:mm, então não precisa atualizar a cada segundo.
        _timerRelogio = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
        _timerRelogio.Tick += (_, _) => AtualizarRelogio();

        // Um DispatcherTimer rodando fica referenciado pelo Dispatcher e mantém esta View (e a
        // MainWindow inteira) viva mesmo depois de fechada — no "Sair" a janela antiga nunca era
        // liberada. Parar no Unloaded resolve; Loaded religa se a View voltar a ser exibida.
        Loaded += (_, _) =>
        {
            AtualizarRelogio();
            _timerRelogio.Start();
        };
        Unloaded += (_, _) => _timerRelogio.Stop();
    }

    private void AtualizarRelogio() => TextoRelogio.Text = DateTime.Now.ToString("HH:mm");
}
