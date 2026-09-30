using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace LivroCaixa.App.Converters;

/// <summary>true → Collapsed, false → Visible (o contrário do BooleanToVisibilityConverter do WPF).</summary>
public class BoolInvertidoParaVisibilidadeConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is true ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
