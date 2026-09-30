using System.Globalization;
using System.Windows.Data;

namespace LivroCaixa.App.Converters;

/// <summary>Verde para valores positivos ou zero, vermelho para negativos — usado no Resultado do período em Relatórios.</summary>
public class ValorParaCorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is decimal valor && valor < 0
            ? System.Windows.Application.Current.Resources["Brush.Error"]
            : System.Windows.Application.Current.Resources["Brush.Success"];

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
