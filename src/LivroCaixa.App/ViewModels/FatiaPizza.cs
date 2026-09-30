using System.Windows.Media;

namespace LivroCaixa.App.ViewModels;

public record FatiaPizza(string Categoria, decimal Total, Brush Cor, Geometry Geometria)
{
    public string TotalExibicao => Total.ToString("C2");
}
