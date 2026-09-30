using LivroCaixa.Domain.Entities;
using LivroCaixa.Domain.Enums;
using LivroCaixa.Domain.Exceptions;

namespace LivroCaixa.Tests.Domain;

public class UsuarioTests
{
    [Fact]
    public void Deve_criar_usuario_com_dados_validos()
    {
        var empresaId = Guid.NewGuid();

        var usuario = new Usuario(empresaId, "João", "hash-fake", PerfilUsuario.Administrador);

        Assert.Equal(empresaId, usuario.EmpresaId);
        Assert.Equal("João", usuario.Nome);
        Assert.Equal(PerfilUsuario.Administrador, usuario.Perfil);
    }

    [Fact]
    public void Deve_rejeitar_empresa_vazia()
    {
        Assert.Throws<DomainException>(() =>
            new Usuario(Guid.Empty, "João", "hash-fake", PerfilUsuario.Administrador));
    }

    [Fact]
    public void Deve_rejeitar_senha_hash_vazia()
    {
        Assert.Throws<DomainException>(() =>
            new Usuario(Guid.NewGuid(), "João", "", PerfilUsuario.Administrador));
    }

    [Fact]
    public void Deve_permitir_redefinir_senha()
    {
        var usuario = new Usuario(Guid.NewGuid(), "João", "hash-antigo", PerfilUsuario.Administrador);

        usuario.RedefinirSenha("hash-novo");

        Assert.Equal("hash-novo", usuario.SenhaHash);
    }
}
