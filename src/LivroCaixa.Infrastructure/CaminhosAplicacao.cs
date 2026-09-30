namespace LivroCaixa.Infrastructure;

public static class CaminhosAplicacao
{
    public static string DiretorioDados => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "LivroCaixa");

    public static string CaminhoBancoDados => Path.Combine(DiretorioDados, "contabil.db");

    public static string DiretorioAnexos => Path.Combine(DiretorioDados, "Anexos");

    public static string DiretorioLogs => Path.Combine(DiretorioDados, "Logs");

    public static void GarantirDiretorios()
    {
        Directory.CreateDirectory(DiretorioDados);
        Directory.CreateDirectory(DiretorioAnexos);
        Directory.CreateDirectory(DiretorioLogs);
    }
}
