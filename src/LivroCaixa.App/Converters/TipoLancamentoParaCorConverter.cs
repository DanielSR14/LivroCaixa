using System.Globalization;
using System.Windows.Data;
using LivroCaixa.Domain.Enums;

namespace LivroCaixa.App.Converters;

/// <summary>Verde para Entrada, vermelho para Saída — mesma convenção usada no extrato de Lançamentos.</summary>
public class TipoLancamentoParaCorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is TipoLancamento.Saida
            ? System.Windows.Application.Current.Resources["Brush.Error"]
            : System.Windows.Application.Current.Resources["Brush.Success"];

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
