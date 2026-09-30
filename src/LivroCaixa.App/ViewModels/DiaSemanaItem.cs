namespace LivroCaixa.App.ViewModels;

/// <summary>Item do calendário da semana atual mostrado no Dashboard.</summary>
public record DiaSemanaItem(string Abreviacao, int Dia, bool EhHoje);
