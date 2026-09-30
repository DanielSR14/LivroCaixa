using Microsoft.Data.Sqlite;

namespace LivroCaixa.Infrastructure.Database;

/// <summary>
/// Responde "já existe empresa cadastrada?" sem o EF Core, para a abertura do app decidir entre
/// login e cadastro sem esperar as migrations e a montagem do modelo (mais de um segundo na partida).
/// Somente leitura: nunca cria o arquivo do banco nem altera nada nele.
/// </summary>
public static class VerificacaoRapidaBanco
{
    public static bool ExisteEmpresaCadastrada(string caminhoBanco)
    {
        if (!File.Exists(caminhoBanco))
            return false;

        // Pooling=False: a conexão é de uma vez só e o arquivo precisa ser liberado logo (armadilha #14).
        var textoConexao = new SqliteConnectionStringBuilder
        {
            DataSource = caminhoBanco,
            Mode = SqliteOpenMode.ReadOnly,
            Pooling = false,
        }.ToString();

        using var conexao = new SqliteConnection(textoConexao);
        conexao.Open();

        // Banco criado mas sem a tabela ainda (migrations interrompidas) conta como "sem empresa".
        // São duas consultas porque o SQLite compila a consulta inteira antes de executar: sem a
        // tabela, um "FROM Empresas" falharia mesmo atrás de um AND.
        if (!ExecutarExiste(conexao, "SELECT 1 FROM sqlite_master WHERE type = 'table' AND name = 'Empresas'"))
            return false;

        return ExecutarExiste(conexao, "SELECT 1 FROM Empresas");
    }

    private static bool ExecutarExiste(SqliteConnection conexao, string consulta)
    {
        using var comando = conexao.CreateCommand();
        comando.CommandText = $"SELECT EXISTS ({consulta})";
        return Convert.ToInt64(comando.ExecuteScalar()) == 1;
    }
}
