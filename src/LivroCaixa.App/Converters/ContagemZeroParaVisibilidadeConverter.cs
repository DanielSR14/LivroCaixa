using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace LivroCaixa.App.Converters;

/// <summary>Mostra o elemento (ex: uma mensagem de "sem dados") quando a contagem vinculada é zero.</summary>
public class ContagemZeroParaVisibilidadeConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is int contagem && contagem == 0 ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
