using LivroCaixa.Domain.Entities;
using LivroCaixa.Infrastructure.Database;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace LivroCaixa.Tests.Infrastructure;

/// <summary>
/// A abertura do app decide login × cadastro com SQL cru, sem o EF Core. Estes testes usam um
/// arquivo real com o esquema das migrations reais: se a tabela Empresas mudar de nome, a consulta
/// crua fica desatualizada e o cliente cairia no cadastro inicial com os dados já existentes.
/// </summary>
public class VerificacaoRapidaBancoTests : IDisposable
{
    private readonly string _caminhoBanco = Path.Combine(Path.GetTempPath(), $"verificacao-rapida-{Guid.NewGuid():N}.db");

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        File.Delete(_caminhoBanco);
    }

    private LivroCaixaDbContext NovoContexto() =>
        new(new DbContextOptionsBuilder<LivroCaixaDbContext>()
            .UseSqlite($"Data Source={_caminhoBanco};Pooling=False").Options);

    [Fact]
    public void Sem_arquivo_de_banco_deve_responder_que_nao_ha_empresa_e_nao_criar_o_arquivo()
    {
        Assert.False(VerificacaoRapidaBanco.ExisteEmpresaCadastrada(_caminhoBanco));
        Assert.False(File.Exists(_caminhoBanco));
    }

    [Fact]
    public void Banco_sem_tabelas_deve_responder_que_nao_ha_empresa()
    {
        using (var conexao = new SqliteConnection($"Data Source={_caminhoBanco};Pooling=False"))
            conexao.Open();

        Assert.False(VerificacaoRapidaBanco.ExisteEmpresaCadastrada(_caminhoBanco));
    }

    [Fact]
    public void Banco_migrado_sem_empresa_deve_responder_que_nao_ha_empresa()
    {
        using (var contexto = NovoContexto())
            contexto.Database.Migrate();

        Assert.False(VerificacaoRapidaBanco.ExisteEmpresaCadastrada(_caminhoBanco));
    }

    [Fact]
    public void Banco_migrado_com_empresa_deve_responder_que_ha_empresa_e_liberar_o_arquivo()
    {
        using (var contexto = NovoContexto())
        {
            contexto.Database.Migrate();
            contexto.Empresas.Add(new Empresa("Sindicato dos Trabalhadores", "12.345.678/0001-90"));
            contexto.SaveChanges();
        }

        Assert.True(VerificacaoRapidaBanco.ExisteEmpresaCadastrada(_caminhoBanco));

        // As migrations rodam logo depois na abertura: a verificação não pode deixar o arquivo preso.
        File.Delete(_caminhoBanco);
        Assert.False(File.Exists(_caminhoBanco));
    }
}
