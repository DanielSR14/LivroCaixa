using LivroCaixa.Application;
using LivroCaixa.Application.Services;
using LivroCaixa.Domain.Entities;
using LivroCaixa.Domain.Enums;
using LivroCaixa.Domain.Exceptions;
using LivroCaixa.Infrastructure.Database;
using LivroCaixa.Infrastructure.Repositories;
using LivroCaixa.Infrastructure.Security;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace LivroCaixa.Tests.Infrastructure;

public class SegurancaServiceFluxoTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly LivroCaixaDbContext _context;
    private readonly BCryptPasswordHasher _hasher = new();
    private readonly ControleTentativasLogin _controleTentativas = new();
    private readonly Usuario _usuario;

    public SegurancaServiceFluxoTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        _context = NovoContexto();
        _context.Database.EnsureCreated();

        var empresa = new Empresa("Sindicato Teste", "12.345.678/0001-90");
        _usuario = new Usuario(empresa.Id, "tesoureiro", _hasher.GerarHash("senha-antiga"), PerfilUsuario.Administrador);
        _context.AddRange(empresa, _usuario);
        _context.SaveChanges();
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    private LivroCaixaDbContext NovoContexto() =>
        new(new DbContextOptionsBuilder<LivroCaixaDbContext>().UseSqlite(_connection).Options);

    private SegurancaService CriarServico(LivroCaixaDbContext contexto) =>
        new(new EmpresaRepository(contexto), new UsuarioRepository(contexto), _hasher, _controleTentativas, contexto);

    private async Task<string> DefinirCodigoAsync(SegurancaService servico)
    {
        var codigo = CodigoRecuperacao.Gerar();
        await servico.DefinirCodigoRecuperacaoAsync(_usuario.Id, codigo);
        return codigo;
    }

    [Fact]
    public async Task Codigo_de_recuperacao_deve_trocar_a_senha_e_valer_uma_unica_vez()
    {
        var servico = CriarServico(_context);
        Assert.True(await servico.PrecisaGerarCodigoRecuperacaoAsync(_usuario.Id));
        var codigo = await DefinirCodigoAsync(servico);
        Assert.False(await servico.PrecisaGerarCodigoRecuperacaoAsync(_usuario.Id));

        // Digitado em minúsculas e sem hífens: deve ser aceito do mesmo jeito.
        var novoCodigo = await servico.RecuperarAcessoAsync("tesoureiro", codigo.Replace("-", "").ToLowerInvariant(), "senha-nova");

        using var outroContexto = NovoContexto();
        var persistido = await new UsuarioRepository(outroContexto).ObterPorIdAsync(_usuario.Id);
        Assert.True(_hasher.VerificarSenha("senha-nova", persistido!.SenhaHash));
        Assert.NotEqual(codigo, novoCodigo);

        await Assert.ThrowsAsync<DomainException>(() => servico.RecuperarAcessoAsync("tesoureiro", codigo, "outra-senha"));
        await servico.RecuperarAcessoAsync("tesoureiro", novoCodigo, "outra-senha");
    }

    [Fact]
    public async Task Codigo_errado_deve_falhar_e_bloquear_apos_tentativas_repetidas_sem_afetar_o_login()
    {
        var servico = CriarServico(_context);
        await DefinirCodigoAsync(servico);

        for (var i = 0; i < 5; i++)
        {
            var erro = await Assert.ThrowsAsync<DomainException>(() =>
                servico.RecuperarAcessoAsync("tesoureiro", "AAAA-BBBB-CCCC-DDDD", "senha-nova"));
            Assert.Equal("Usuário ou código de recuperação inválidos.", erro.Message);
        }

        var bloqueio = await Assert.ThrowsAsync<DomainException>(() =>
            servico.RecuperarAcessoAsync("tesoureiro", "AAAA-BBBB-CCCC-DDDD", "senha-nova"));
        Assert.StartsWith("Muitas tentativas erradas", bloqueio.Message);
        Assert.Null(_controleTentativas.ObterTempoRestanteDeBloqueio("tesoureiro"));
    }

    [Fact]
    public async Task Recuperacao_nao_deve_aceitar_senha_nova_curta()
    {
        var servico = CriarServico(_context);
        var codigo = await DefinirCodigoAsync(servico);

        await Assert.ThrowsAsync<DomainException>(() => servico.RecuperarAcessoAsync("tesoureiro", codigo, "123"));
    }

    [Fact]
    public async Task Confirmar_senha_do_desbloqueio_deve_validar_a_senha_atual()
    {
        var servico = CriarServico(_context);

        Assert.True(await servico.ConfirmarSenhaAsync(_usuario.Id, "senha-antiga"));
        Assert.False(await servico.ConfirmarSenhaAsync(_usuario.Id, "errada"));
    }

    [Fact]
    public async Task Bloqueio_por_inatividade_e_aceite_do_termo_devem_persistir()
    {
        var servico = CriarServico(_context);
        Assert.Equal(Empresa.BloqueioInatividadePadraoMinutos, await servico.ObterMinutosBloqueioInatividadeAsync());
        Assert.True(await servico.PrecisaAceitarTermoAsync("1"));

        await servico.DefinirMinutosBloqueioInatividadeAsync(0);
        await servico.RegistrarAceiteTermoAsync("1");
        await Assert.ThrowsAsync<DomainException>(() => servico.DefinirMinutosBloqueioInatividadeAsync(7));

        using var outroContexto = NovoContexto();
        var servicoNovo = CriarServico(outroContexto);
        Assert.Equal(0, await servicoNovo.ObterMinutosBloqueioInatividadeAsync());
        Assert.False(await servicoNovo.PrecisaAceitarTermoAsync("1"));
        Assert.True(await servicoNovo.PrecisaAceitarTermoAsync("2"));
    }

    [Fact]
    public void Codigo_gerado_deve_ter_formato_legivel_e_sem_caracteres_ambiguos()
    {
        var codigo = CodigoRecuperacao.Gerar();

        Assert.Matches("^[A-Z2-9]{4}-[A-Z2-9]{4}-[A-Z2-9]{4}-[A-Z2-9]{4}$", codigo);
        Assert.DoesNotContain(codigo, c => c is 'O' or 'I' or 'L' or '0' or '1');
        Assert.NotEqual(codigo, CodigoRecuperacao.Gerar());
    }
}
