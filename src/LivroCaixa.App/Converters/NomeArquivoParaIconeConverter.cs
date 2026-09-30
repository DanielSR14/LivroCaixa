using System.Globalization;
using System.Windows.Data;

namespace LivroCaixa.App.Converters;

/// <summary>Ícone pelo tipo de arquivo (extensão) de um anexo — pdf/jpg/jpeg têm ícone próprio;
/// qualquer outra extensão (ex: png) cai num ícone de documento genérico.</summary>
public class NomeArquivoParaIconeConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var extensao = System.IO.Path.GetExtension(value as string)?.TrimStart('.').ToLowerInvariant();
        return extensao switch
        {
            "pdf" => "/Assets/Icons/arquivo-pdf.svg",
            "jpg" => "/Assets/Icons/arquivo-jpg.svg",
            "jpeg" => "/Assets/Icons/arquivo-jpeg.svg",
            _ => "/Assets/Icons/file-text.svg"
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
