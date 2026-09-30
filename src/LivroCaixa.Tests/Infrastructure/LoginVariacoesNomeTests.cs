using LivroCaixa.Application;
using LivroCaixa.Application.DTOs;
using LivroCaixa.Application.Services;
using LivroCaixa.Domain.Exceptions;
using LivroCaixa.Infrastructure.Database;
using LivroCaixa.Infrastructure.Repositories;
using LivroCaixa.Infrastructure.Security;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace LivroCaixa.Tests.Infrastructure;

/// <summary>
/// Nome de usuário digitado de outro jeito (maiúsculas, espaço sobrando) não pode travar o login.
/// Contra SQLite real de propósito: a comparação "=" do SQLite diferencia maiúsculas, e o LOWER()
/// dele só conhece ASCII ("JOÃO" viraria "joÃo") — um fake em memória esconderia isso.
/// </summary>
public class LoginVariacoesNomeTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly LivroCaixaDbContext _context;
    private readonly BCryptPasswordHasher _hasher = new();

    public LoginVariacoesNomeTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        _context = new LivroCaixaDbContext(new DbContextOptionsBuilder<LivroCaixaDbContext>().UseSqlite(_connection).Options);
        _context.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    private async Task CadastrarAsync(string nomeUsuario) =>
        await new CadastroInicialService(
                new EmpresaRepository(_context), new UsuarioRepository(_context), new ContaRepository(_context),
                new CategoriaRepository(_context), _hasher, _context)
            .CadastrarAsync(new CadastroInicialDto("Sindicato Teste", "12.345.678/0001-90", null, null, nomeUsuario, "senha123"));

    private AutenticacaoService CriarAutenticacao() =>
        new(new EmpresaRepository(_context), new UsuarioRepository(_context), _hasher, new ControleTentativasLogin());

    [Theory]
    [InlineData("João Silva")]
    [InlineData("joão silva")]
    [InlineData("JOÃO SILVA")]
    [InlineData("  João Silva  ")]
    public async Task Deve_entrar_com_o_nome_digitado_com_outras_maiusculas_ou_espacos(string digitado)
    {
        await CadastrarAsync("João Silva");

        var resultado = await CriarAutenticacao().AutenticarAsync(new LoginDto(digitado, "senha123"));

        Assert.NotNull(resultado);
        Assert.Equal("João Silva", resultado.Value.Usuario.Nome);
    }

    [Fact]
    public async Task Variacao_do_nome_nao_deve_dispensar_a_senha_correta()
    {
        await CadastrarAsync("João Silva");

        Assert.Null(await CriarAutenticacao().AutenticarAsync(new LoginDto("joão silva", "SENHA123")));
    }

    [Fact]
    public async Task Senha_nao_pode_ficar_gravada_em_texto_no_banco()
    {
        await CadastrarAsync("João Silva");

        using var comando = _connection.CreateCommand();
        comando.CommandText = "SELECT SenhaHash FROM Usuarios";
        var gravado = (string)comando.ExecuteScalar()!;

        Assert.DoesNotContain("senha123", gravado);
        Assert.StartsWith("$2", gravado); // prefixo de hash BCrypt
    }

    [Fact]
    public async Task Recuperacao_de_acesso_tambem_deve_aceitar_variacoes_do_nome()
    {
        await CadastrarAsync("João Silva");
        var usuario = _context.Usuarios.Single();
        var seguranca = new SegurancaService(
            new EmpresaRepository(_context), new UsuarioRepository(_context), _hasher, new ControleTentativasLogin(), _context);
        var codigo = CodigoRecuperacao.Gerar();
        await seguranca.DefinirCodigoRecuperacaoAsync(usuario.Id, codigo);

        await seguranca.RecuperarAcessoAsync(" JOÃO SILVA ", codigo, "nova-senha");

        Assert.NotNull(await CriarAutenticacao().AutenticarAsync(new LoginDto("João Silva", "nova-senha")));
    }

    [Fact]
    public async Task Bloqueio_por_tentativas_deve_valer_para_qualquer_variacao_do_nome()
    {
        await CadastrarAsync("João Silva");
        var autenticacao = CriarAutenticacao();

        for (var i = 0; i < 5; i++)
            await autenticacao.AutenticarAsync(new LoginDto(i % 2 == 0 ? "joão silva" : " JOÃO SILVA", "errada"));

        await Assert.ThrowsAsync<DomainException>(() =>
            autenticacao.AutenticarAsync(new LoginDto("João Silva", "senha123")));
    }
}
