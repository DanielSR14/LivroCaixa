using System.IO;
using System.Windows;
using System.Windows.Media;
using LivroCaixa.App.ViewModels;
using Microsoft.Win32;

namespace LivroCaixa.App.Views;

public partial class LancamentoWindow : Window
{
    private readonly LancamentoFormViewModel _viewModel;

    public LancamentoWindow(LancamentoFormViewModel viewModel)
    {
        InitializeComponent();

        _viewModel = viewModel;
        DataContext = _viewModel;
        _viewModel.Concluido += (_, _) =>
        {
            DialogResult = true;
            Close();
        };

        Loaded += async (_, _) => await _viewModel.InicializarAsync();
    }

    private async void BotaoAnexar_Click(object sender, RoutedEventArgs e)
    {
        var podeAnexar = await _viewModel.GarantirLancamentoCriadoAsync();
        if (!podeAnexar)
            return;

        var dialogo = new OpenFileDialog
        {
            Title = "Selecionar comprovante",
            Filter = "Documentos suportados (*.pdf;*.jpg;*.jpeg;*.png)|*.pdf;*.jpg;*.jpeg;*.png"
        };

        if (dialogo.ShowDialog(this) == true)
            await _viewModel.AdicionarAnexoAsync(dialogo.FileName);
    }

    // Eventos "Preview" na janela inteira: os TextBox tratam o arraste por conta própria (esperam
    // texto) e, sem interceptar antes deles, soltar um arquivo em cima de um campo não fazia nada.
    private void Janela_PreviewDragOver(object sender, DragEventArgs e)
    {
        var temArquivo = e.Data.GetDataPresent(DataFormats.FileDrop);
        e.Effects = temArquivo ? DragDropEffects.Copy : DragDropEffects.None;
        DestacarAreaSoltar(temArquivo);
        e.Handled = true;
    }

    private void Janela_PreviewDragLeave(object sender, DragEventArgs e) => DestacarAreaSoltar(false);

    private async void Janela_PreviewDrop(object sender, DragEventArgs e)
    {
        DestacarAreaSoltar(false);
        if (e.Data.GetData(DataFormats.FileDrop) is not string[] arquivos || arquivos.Length == 0)
            return;

        e.Handled = true;
        if (!await _viewModel.GarantirLancamentoCriadoAsync())
            return;

        // Tipo e tamanho são validados no AnexoService (mesma regra do "Procurar..."). Pastas soltas
        // junto com arquivos são ignoradas.
        await _viewModel.AdicionarAnexosAsync(arquivos.Where(File.Exists).ToList());
    }

    private void DestacarAreaSoltar(bool destacar)
    {
        AreaSoltarComprovante.BorderBrush = (Brush)FindResource(destacar ? "Brush.Primary" : "Brush.Border");
        AreaSoltarComprovante.Background = destacar ? (Brush)FindResource("Brush.PrimaryTint") : Brushes.Transparent;
    }
}
