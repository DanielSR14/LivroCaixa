namespace LivroCaixa.App.ViewModels;

public record EvolucaoMensalItem(
    string MesExibicao,
    decimal Entradas,
    decimal Saidas,
    double PercentualEntradas,
    double PercentualSaidas);
