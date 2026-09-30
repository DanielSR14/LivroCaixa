namespace LivroCaixa.Application.Interfaces;

public interface IPasswordHasher
{
    string GerarHash(string senha);

    bool VerificarSenha(string senha, string hash);
}
