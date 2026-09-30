namespace LivroCaixa.App.ViewModels;

public record ItemTempoBloqueio(int Minutos, string Nome)
{
    public static ItemTempoBloqueio De(int minutos) =>
        new(minutos, minutos == 0 ? "Nunca bloquear" : $"Após {minutos} minutos sem uso");
}
