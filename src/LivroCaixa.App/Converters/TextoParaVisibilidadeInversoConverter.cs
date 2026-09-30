using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace LivroCaixa.App.Converters;

/// <summary>Inverso de <see cref="TextoParaVisibilidadeConverter"/>: mostra o elemento quando o
/// texto vinculado está vazio, esconde quando preenchido.</summary>
public class TextoParaVisibilidadeInversoConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        string.IsNullOrWhiteSpace(value as string) ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
