using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace LivroCaixa.App.Converters;

/// <summary>Visible quando o nome da conta bate com um banco conhecido (tem logo real);
/// Collapsed quando não bate (nesse caso, quem mostra ícone é o TipoContaParaIconeConverter,
/// como fallback). Usar sempre o inverso (InverterVisibilidade=true) no elemento irmão.</summary>
public class NomeContaTemLogoBancoConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var temLogo = BancosConhecidos.ObterIcone(value as string) is not null;
        if (parameter is string p && p.Equals("Inverso", StringComparison.OrdinalIgnoreCase))
            temLogo = !temLogo;

        return temLogo ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
