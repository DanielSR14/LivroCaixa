using System.Globalization;
using System.Windows.Data;

namespace LivroCaixa.App.Converters;

/// <summary>Devolve o caminho do logo do banco (Assets/BancosLogos) quando o nome da conta bate
/// com um banco conhecido (ver BancosConhecidos), ou null quando não há correspondência.</summary>
public class NomeContaParaIconeBancoConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        BancosConhecidos.ObterIcone(value as string);

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
