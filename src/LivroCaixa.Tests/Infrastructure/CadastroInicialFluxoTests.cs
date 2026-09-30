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

public class CadastroInicialFluxoTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly LivroCaixaDbContext _context;

    public CadastroInicialFluxoTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<LivroCaixaDbContext>().UseSqlite(_connection).Options;
        _context = new LivroCaixaDbContext(options);
        _context.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    private CadastroInicialService CriarCadastroInicialService() => new(
        new EmpresaRepository(_context),
        new UsuarioRepository(_context),
        new ContaRepository(_context),
        new CategoriaRepository(_context),
        new BCryptPasswordHasher(),
        _context);

    [Fact]
    public async Task Deve_cadastrar_empresa_e_permitir_login_em_seguida_usando_repositorios_reais()
    {
        var dto = new CadastroInicialDto("Padaria Pão Quente", "12.345.678/0001-90", null, "Comércio", "João", "senha123");
        await CriarCadastroInicialService().CadastrarAsync(dto);

        var autenticacaoService = new AutenticacaoService(
            new EmpresaRepository(_context),
            new UsuarioRepository(_context),
            new BCryptPasswordHasher(),
            new ControleTentativasLogin());

        var resultado = await autenticacaoService.AutenticarAsync(new LoginDto("João", "senha123"));

        Assert.NotNull(resultado);
        Assert.Equal("Padaria Pão Quente", resultado.Value.Empresa.RazaoSocial);
    }

    [Fact]
    public async Task Deve_rejeitar_segunda_empresa_no_mesmo_banco()
    {
        await CriarCadastroInicialService().CadastrarAsync(
            new CadastroInicialDto("Padaria Pão Quente", "12.345.678/0001-90", null, null, "João", "senha123"));

        await Assert.ThrowsAsync<DomainException>(() => CriarCadastroInicialService().CadastrarAsync(
            new CadastroInicialDto("Outra Empresa", "98.765.432/0001-10", null, null, "Maria", "senha123")));
    }
}
