using System.IO;
using LivroCaixa.Infrastructure;

namespace LivroCaixa.App;

/// <summary>
/// Persistência local (não é dado de negócio, não passa pelo EF Core) do nome de usuário
/// lembrado na tela de login — um arquivo de texto simples ao lado do banco de dados.
/// </summary>
public static class PreferenciasLogin
{
    private static string CaminhoArquivo => Path.Combine(CaminhosAplicacao.DiretorioDados, "usuario_lembrado.txt");

    public static string? ObterUsuarioLembrado()
    {
        try
        {
            return File.Exists(CaminhoArquivo) ? File.ReadAllText(CaminhoArquivo).Trim() : null;
        }
        catch (IOException)
        {
            return null;
        }
    }

    public static void SalvarUsuarioLembrado(string? nomeUsuario)
    {
        try
        {
            CaminhosAplicacao.GarantirDiretorios();
            if (string.IsNullOrWhiteSpace(nomeUsuario))
                File.Delete(CaminhoArquivo);
            else
                File.WriteAllText(CaminhoArquivo, nomeUsuario.Trim());
        }
        catch (IOException)
        {
            // Preferência de conveniência — falha ao gravar não deve impedir o login.
        }
    }
}
