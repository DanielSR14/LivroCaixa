using System.Globalization;
using System.Windows.Data;
using LivroCaixa.Domain.Enums;

namespace LivroCaixa.App.Converters;

/// <summary>Ícone genérico por tipo de conta (Caixa/Cartão/Investimento) — usado como fallback
/// quando o nome da conta não bate com nenhum banco conhecido (ver NomeContaTemLogoBancoConverter
/// e BancosConhecidos). Banco sem nome reconhecido não tem ícone próprio, então retorna null.</summary>
public class TipoContaParaIconeConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is TipoConta tipo
            ? tipo switch
            {
                TipoConta.Caixa => "/Assets/Icons/tipo-caixa.svg",
                TipoConta.Cartao => "/Assets/Icons/tipo-cartao.svg",
                TipoConta.Investimento => "/Assets/Icons/tipo-investimento.svg",
                _ => null
            }
            : null;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
