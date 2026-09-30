using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows.Data;

namespace LivroCaixa.App.Converters;

/// <summary>Insere espaço antes de cada letra maiúscula (exceto a primeira) do nome de um enum,
/// para exibição — ex: "TransferenciaBancaria" vira "Transferencia Bancaria". Enums de uma
/// palavra só (ex: "Pix") passam sem alteração.</summary>
public class EnumParaTextoExibicaoConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is null ? string.Empty : Regex.Replace(value.ToString()!, "(?<!^)([A-Z])", " $1");

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
