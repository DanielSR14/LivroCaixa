using System.Globalization;
using System.Windows.Data;
using LivroCaixa.Domain.Enums;

namespace LivroCaixa.App.Converters;

public class FormaPagamentoParaIconeConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is FormaPagamento forma
            ? forma switch
            {
                FormaPagamento.Dinheiro => "/Assets/Icons/dinheiro.svg",
                FormaPagamento.Pix => "/Assets/Icons/pix.svg",
                FormaPagamento.Cartao => "/Assets/Icons/cartao.svg",
                FormaPagamento.Boleto => "/Assets/Icons/boleto.svg",
                FormaPagamento.TransferenciaBancaria => "/Assets/Icons/transferencia.svg",
                _ => "/Assets/Icons/outro-pagamento.svg"
            }
            : "/Assets/Icons/outro-pagamento.svg";

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
