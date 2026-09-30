using LivroCaixa.Application.DTOs;
using LivroCaixa.Application.Services;
using LivroCaixa.Domain.Enums;
using LivroCaixa.Domain.Exceptions;
using LivroCaixa.Infrastructure.Security;
using LivroCaixa.Tests.Application.Fakes;

namespace LivroCaixa.Tests.Application;

public class CadastroInicialServiceTests
{
    private static CadastroInicialService CriarServico(
        FakeEmpresaRepository? empresas = null,
        FakeUsuarioRepository? usuarios = null,
        FakeContaRepository? contas = null,
        FakeCategoriaRepository? categorias = null)
    {
        return new CadastroInicialService(
            empresas ?? new FakeEmpresaRepository(),
            usuarios ?? new FakeUsuarioRepository(),
            contas ?? new FakeContaRepository(),
            categorias ?? new FakeCategoriaRepository(),
            new BCryptPasswordHasher(),
            new FakeUnitOfWork());
    }

    [Fact]
    public async Task Deve_criar_empresa_usuario_conta_caixa_e_categorias_padrao()
    {
        var empresas = new FakeEmpresaRepository();
        var usuarios = new FakeUsuarioRepository();
        var contas = new FakeContaRepository();
        var categorias = new FakeCategoriaRepository();
        var servico = CriarServico(empresas, usuarios, contas, categorias);

        var dto = new CadastroInicialDto("Padaria Pão Quente", "12.345.678/0001-90", "Padaria Pão Quente", "Comércio", "João", "senha123");

        var usuarioCriado = await servico.CadastrarAsync(dto);

        Assert.Single(empresas.Empresas);
        Assert.Single(usuarios.Usuarios);
        Assert.Equal(PerfilUsuario.Administrador, usuarioCriado.Perfil);
        Assert.Single(contas.Contas);
        Assert.Equal("Caixa", contas.Contas[0].Nome);
        // Segmento "Comércio" usa o modelo de categorias de comércio (ver CategoriasPadrao), não o genérico.
        Assert.Equal(13, categorias.Categorias.Count);
    }

    [Fact]
    public async Task Deve_usar_categorias_genericas_quando_segmento_nao_informado()
    {
        var categorias = new FakeCategoriaRepository();
        var servico = CriarServico(categorias: categorias);
        var dto = new CadastroInicialDto("Padaria Pão Quente", "12.345.678/0001-90", null, null, "João", "senha123");

        await servico.CadastrarAsync(dto);

        Assert.Equal(14, categorias.Categorias.Count);
    }

    [Fact]
    public async Task Nao_deve_permitir_cadastrar_duas_empresas()
    {
        var empresas = new FakeEmpresaRepository();
        var primeiroDto = new CadastroInicialDto("Padaria Pão Quente", "12.345.678/0001-90", null, null, "João", "senha123");
        await CriarServico(empresas).CadastrarAsync(primeiroDto);

        var segundoDto = new CadastroInicialDto("Outra Empresa", "98.765.432/0001-10", null, null, "Maria", "senha123");

        await Assert.ThrowsAsync<DomainException>(() => CriarServico(empresas).CadastrarAsync(segundoDto));
    }

    [Fact]
    public async Task Deve_rejeitar_senha_curta()
    {
        var servico = CriarServico();
        var dto = new CadastroInicialDto("Padaria Pão Quente", "12.345.678/0001-90", null, null, "João", "123");

        await Assert.ThrowsAsync<DomainException>(() => servico.CadastrarAsync(dto));
    }
}
