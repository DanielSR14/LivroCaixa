using System.Globalization;
using System.Windows.Data;

namespace LivroCaixa.App.Converters;

public class InverterBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        !(value is true);

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        !(value is true);
}
