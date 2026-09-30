using LivroCaixa.Domain.Entities;
using LivroCaixa.Domain.Enums;
using LivroCaixa.Infrastructure.Database;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace LivroCaixa.Tests.Infrastructure;

/// <summary>
/// Os demais testes criam o banco com EnsureCreated(), que ignora as migrations — mas é Migrate()
/// que roda na máquina do cliente a cada atualização do app. Aqui as migrations reais são aplicadas,
/// inclusive sobre um banco "de versão antiga" com dados, que é exatamente o caso de risco.
/// </summary>
public class MigracoesTests : IDisposable
{
    // Última migration antes da leva de 2026-09-29 (segurança, modelos, transferências).
    private const string MigrationAnteriorAosNovosRecursos = "20260923173031_AdicionaAuditoriaEBackup";

    private readonly SqliteConnection _connection;

    public MigracoesTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
    }

    public void Dispose() => _connection.Dispose();

    private LivroCaixaDbContext NovoContexto() =>
        new(new DbContextOptionsBuilder<LivroCaixaDbContext>().UseSqlite(_connection).Options);

    private void Executar(string sql)
    {
        using var comando = _connection.CreateCommand();
        comando.CommandText = sql;
        comando.ExecuteNonQuery();
    }

    [Fact]
    public void Migrations_devem_criar_o_banco_do_zero_sem_deixar_nada_pendente()
    {
        using var contexto = NovoContexto();

        contexto.Database.Migrate();

        Assert.Empty(contexto.Database.GetPendingMigrations());
        Assert.Equal(contexto.Database.GetMigrations(), contexto.Database.GetAppliedMigrations());
    }

    /// <summary>
    /// Pega o erro de mudar uma entidade/configuração e esquecer de gerar a migration: o app compila,
    /// os testes com EnsureCreated passam, mas no cliente a coluna nova simplesmente não existe.
    /// </summary>
    [Fact]
    public void Snapshot_das_migrations_deve_estar_em_dia_com_o_modelo_atual()
    {
        using var contexto = NovoContexto();
        var snapshot = contexto.GetService<IMigrationsAssembly>().ModelSnapshot!.Model;
        var modeloSnapshot = contexto.GetService<IModelRuntimeInitializer>()
            .Initialize(snapshot is IMutableModel mutavel ? mutavel.FinalizeModel() : snapshot);
        var modeloAtual = contexto.GetService<IDesignTimeModel>().Model;

        var diferencas = contexto.GetService<IMigrationsModelDiffer>()
            .GetDifferences(modeloSnapshot.GetRelationalModel(), modeloAtual.GetRelationalModel());

        Assert.True(diferencas.Count == 0,
            "Modelo mudou sem migration: " + string.Join(", ", diferencas.Select(d => d.GetType().Name)));
    }

    [Fact]
    public void Atualizar_banco_de_versao_antiga_deve_preservar_dados_e_aplicar_padroes_novos()
    {
        using (var contextoAntigo = NovoContexto())
            contextoAntigo.GetService<IMigrator>().Migrate(MigrationAnteriorAosNovosRecursos);

        // Dados como um cliente teria antes da atualização (SQL puro: o modelo C# atual já tem
        // colunas que ainda não existem nesta versão do banco).
        var empresaId = Guid.NewGuid().ToString().ToUpperInvariant();
        var usuarioId = Guid.NewGuid().ToString().ToUpperInvariant();
        var contaId = Guid.NewGuid().ToString().ToUpperInvariant();
        Executar($"INSERT INTO Empresas (Id, RazaoSocial, Cnpj, NomeFantasia, Segmento, DataCadastro) VALUES ('{empresaId}', 'Sindicato Antigo', '12.345.678/0001-90', NULL, 'Associação ou Sindicato', '2026-09-22 10:00:00')");
        Executar($"INSERT INTO Usuarios (Id, EmpresaId, Nome, SenhaHash, Perfil, CriadoEm) VALUES ('{usuarioId}', '{empresaId}', 'tesoureiro', '$2a$11$hash', 'Administrador', '2026-09-22 10:00:00')");
        Executar($"INSERT INTO Contas (Id, EmpresaId, Nome, Tipo, SaldoInicial, Agencia, NumeroConta, TipoContaBancaria) VALUES ('{contaId}', '{empresaId}', 'Caixa', 'Caixa', '150.75', NULL, NULL, NULL)");

        using (var contextoNovo = NovoContexto())
            contextoNovo.Database.Migrate();

        using var contexto = NovoContexto();
        var empresa = contexto.Empresas.Single();
        Assert.Equal("Sindicato Antigo", empresa.RazaoSocial);
        // Editado à mão na migration: o gerado seria 0 ("nunca bloquear") para quem já usava o app.
        Assert.Equal(Empresa.BloqueioInatividadePadraoMinutos, empresa.MinutosBloqueioInatividade);
        Assert.Null(empresa.VersaoTermoAceita); // quem já usava vê o termo no próximo login
        Assert.False(contexto.Usuarios.Single().TemCodigoRecuperacao);
        Assert.Equal(150.75m, contexto.Contas.Single().SaldoInicial);

        // As tabelas novas precisam aceitar dados ligados aos registros antigos.
        var conta = contexto.Contas.Single();
        var outraConta = new Conta(empresa.Id, "Banco", TipoConta.Banco);
        var categoria = new Categoria(empresa.Id, "Aluguel", TipoLancamento.Saida);
        contexto.AddRange(outraConta, categoria);
        contexto.Transferencias.Add(new Transferencia(empresa.Id, conta, outraConta, DateOnly.FromDateTime(DateTime.Today), 10m));
        contexto.ModelosLancamento.Add(new ModeloLancamento(empresa.Id, "ALU", "Aluguel", categoria, conta, FormaPagamento.Boleto, 100m, null));
        contexto.SaveChanges();
    }

    [Fact]
    public void Chaves_estrangeiras_devem_estar_ativas_no_banco_migrado()
    {
        using var contexto = NovoContexto();
        contexto.Database.Migrate();

        // Sem PRAGMA foreign_keys o SQLite aceitaria lançamento apontando para conta inexistente,
        // e as regras "não excluir conta com lançamentos" só existiriam na aplicação.
        var erro = Assert.Throws<SqliteException>(() => Executar(
            "INSERT INTO Contas (Id, EmpresaId, Nome, Tipo, SaldoInicial) VALUES ('X', 'EMPRESA-QUE-NAO-EXISTE', 'Caixa', 'Caixa', '0')"));
        Assert.Contains("FOREIGN KEY", erro.Message);
    }
}
