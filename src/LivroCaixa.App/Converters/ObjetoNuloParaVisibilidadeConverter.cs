using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace LivroCaixa.App.Converters;

/// <summary>Mostra o elemento quando o valor vinculado não é null (útil para campos opcionais
/// que não são string, como um DateTime? — TextoParaVisibilidadeConverter não serve para esses,
/// porque só reconhece string).</summary>
public class ObjetoNuloParaVisibilidadeConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is null ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
