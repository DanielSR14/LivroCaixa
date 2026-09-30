namespace LivroCaixa.App.ViewModels;

public record ItemArmazenamentoCompetencia(
    int Ano,
    int Mes,
    int QuantidadeLancamentos,
    int QuantidadeAnexos,
    long TamanhoTotalBytes,
    bool JaExportada)
{
    public string CompetenciaExibicao => $"{Mes:D2}/{Ano:D4}";

    public string StatusExportacaoExibicao => JaExportada ? "Já exportada" : "Ainda não exportada";

    public string TamanhoExibicao => FormatarTamanho(TamanhoTotalBytes);

    public static string FormatarTamanho(long bytes) => bytes switch
    {
        >= 1024 * 1024 => $"{bytes / (1024.0 * 1024.0):0.0} MB",
        >= 1024 => $"{bytes / 1024.0:0.0} KB",
        _ => $"{bytes} bytes"
    };
}
