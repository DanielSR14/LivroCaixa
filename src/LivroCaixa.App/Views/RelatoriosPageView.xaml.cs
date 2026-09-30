using System.Windows.Controls;
using LivroCaixa.App.ViewModels;

namespace LivroCaixa.App.Views;

public partial class RelatoriosPageView : UserControl
{
    public RelatoriosPageView(RelatorioViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
