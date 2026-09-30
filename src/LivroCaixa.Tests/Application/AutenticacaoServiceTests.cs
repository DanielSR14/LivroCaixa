using LivroCaixa.Application;
using LivroCaixa.Application.DTOs;
using LivroCaixa.Application.Services;
using LivroCaixa.Domain.Entities;
using LivroCaixa.Domain.Enums;
using LivroCaixa.Domain.Exceptions;
using LivroCaixa.Infrastructure.Security;
using LivroCaixa.Tests.Application.Fakes;

namespace LivroCaixa.Tests.Application;

public class AutenticacaoServiceTests
{
    private static (AutenticacaoService Servico, Empresa Empresa, Usuario Usuario) CriarCenario()
    {
        var hasher = new BCryptPasswordHasher();
        var empresa = new Empresa("Padaria Pão Quente", "12.345.678/0001-90");
        var usuario = new Usuario(empresa.Id, "João", hasher.GerarHash("senha123"), PerfilUsuario.Administrador);

        var empresas = new FakeEmpresaRepository();
        empresas.Empresas.Add(empresa);
        var usuarios = new FakeUsuarioRepository();
        usuarios.Usuarios.Add(usuario);

        return (new AutenticacaoService(empresas, usuarios, hasher, new ControleTentativasLogin()), empresa, usuario);
    }

    [Fact]
    public async Task Deve_autenticar_usuario_com_senha_correta()
    {
        var (servico, _, usuario) = CriarCenario();

        var resultado = await servico.AutenticarAsync(new LoginDto("João", "senha123"));

        Assert.NotNull(resultado);
        Assert.Equal(usuario.Id, resultado.Value.Usuario.Id);
    }

    [Fact]
    public async Task Deve_rejeitar_senha_incorreta()
    {
        var (servico, _, _) = CriarCenario();

        var resultado = await servico.AutenticarAsync(new LoginDto("João", "senha-errada"));

        Assert.Null(resultado);
    }

    [Fact]
    public async Task Deve_indicar_que_nao_ha_empresa_cadastrada_quando_repositorio_esta_vazio()
    {
        var servico = new AutenticacaoService(
            new FakeEmpresaRepository(),
            new FakeUsuarioRepository(),
            new BCryptPasswordHasher(),
            new ControleTentativasLogin());

        Assert.False(await servico.ExisteEmpresaCadastradaAsync());
    }

    [Fact]
    public async Task Deve_bloquear_apos_varias_tentativas_erradas_seguidas()
    {
        var (servico, _, _) = CriarCenario();

        for (var i = 0; i < 5; i++)
            await servico.AutenticarAsync(new LoginDto("João", "senha-errada"));

        var excecao = await Assert.ThrowsAsync<DomainException>(
            () => servico.AutenticarAsync(new LoginDto("João", "senha123")));

        Assert.Contains("Muitas tentativas", excecao.Message);
    }

    [Fact]
    public async Task Login_com_sucesso_deve_zerar_contagem_de_tentativas_erradas()
    {
        var (servico, _, _) = CriarCenario();

        await servico.AutenticarAsync(new LoginDto("João", "senha-errada"));
        await servico.AutenticarAsync(new LoginDto("João", "senha-errada"));
        await servico.AutenticarAsync(new LoginDto("João", "senha123"));

        // Depois de logar certo, a contagem zera — não deve bloquear em seguida.
        var resultado = await servico.AutenticarAsync(new LoginDto("João", "senha123"));
        Assert.NotNull(resultado);
    }
}
