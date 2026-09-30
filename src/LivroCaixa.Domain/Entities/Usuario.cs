using LivroCaixa.Domain.Enums;
using LivroCaixa.Domain.Exceptions;

namespace LivroCaixa.Domain.Entities;

public class Usuario : EntidadeBase
{
    public Guid EmpresaId { get; private set; }
    public string Nome { get; private set; } = null!;
    public string SenhaHash { get; private set; } = null!;
    public PerfilUsuario Perfil { get; private set; }
    public DateTime CriadoEm { get; private set; }

    /// <summary>
    /// Hash (nunca o texto) do código de recuperação de acesso — a saída para senha esquecida sem
    /// depender de internet/e-mail. Uso único: ao ser usado, um código novo substitui este.
    /// </summary>
    public string? CodigoRecuperacaoHash { get; private set; }

    public bool TemCodigoRecuperacao => CodigoRecuperacaoHash is not null;

    private Usuario()
    {
    }

    public Usuario(Guid empresaId, string nome, string senhaHash, PerfilUsuario perfil)
    {
        if (empresaId == Guid.Empty)
            throw new DomainException("O usuário precisa estar vinculado a uma empresa.");

        if (string.IsNullOrWhiteSpace(nome))
            throw new DomainException("O nome do usuário é obrigatório.");

        if (string.IsNullOrWhiteSpace(senhaHash))
            throw new DomainException("A senha do usuário é obrigatória.");

        EmpresaId = empresaId;
        Nome = nome.Trim();
        SenhaHash = senhaHash;
        Perfil = perfil;
        CriadoEm = DateTime.UtcNow;
    }

    public void RedefinirSenha(string novaSenhaHash)
    {
        if (string.IsNullOrWhiteSpace(novaSenhaHash))
            throw new DomainException("A nova senha do usuário é obrigatória.");

        SenhaHash = novaSenhaHash;
    }

    public void DefinirCodigoRecuperacao(string codigoRecuperacaoHash)
    {
        if (string.IsNullOrWhiteSpace(codigoRecuperacaoHash))
            throw new DomainException("O código de recuperação é obrigatório.");

        CodigoRecuperacaoHash = codigoRecuperacaoHash;
    }
}
