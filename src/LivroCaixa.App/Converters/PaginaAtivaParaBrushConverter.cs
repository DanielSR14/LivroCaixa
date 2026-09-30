using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using LivroCaixa.App.ViewModels;

namespace LivroCaixa.App.Converters;

/// <summary>
/// Compara a página atualmente selecionada (value) com o nome da página representada por
/// este item de menu (parameter) e retorna o brush de destaque quando coincidem, ou
/// transparente caso contrário — usado para realçar o item ativo na sidebar.
/// </summary>
public class PaginaAtivaParaBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is PaginaShell paginaAtual && parameter is string nomePagina &&
            Enum.TryParse<PaginaShell>(nomePagina, out var paginaDoItem) && paginaAtual == paginaDoItem)
        {
            return System.Windows.Application.Current.Resources["Brush.SidebarItemAtivo"];
        }

        return Brushes.Transparent;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
