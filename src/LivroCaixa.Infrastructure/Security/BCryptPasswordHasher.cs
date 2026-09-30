using LivroCaixa.Application.Interfaces;

namespace LivroCaixa.Infrastructure.Security;

public class BCryptPasswordHasher : IPasswordHasher
{
    public string GerarHash(string senha) => BCrypt.Net.BCrypt.HashPassword(senha);

    public bool VerificarSenha(string senha, string hash) => BCrypt.Net.BCrypt.Verify(senha, hash);
}
