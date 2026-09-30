using LivroCaixa.Domain.Entities;

namespace LivroCaixa.Application.Interfaces;

public interface ILancamentoRepository
{
    Task AdicionarAsync(Lancamento lancamento);

    /// <summary>
    /// Marca um Anexo recém-criado como "a ser inserido" explicitamente. Necessário porque
    /// o Anexo já chega com um Id (Guid) atribuído no construtor: sem isso, o EF Core o
    /// descobre apenas via a coleção de navegação do Lancamento e assume, incorretamente,
    /// que a linha já existe no banco (gerando um UPDATE em vez de um INSERT).
    /// </summary>
    void RegistrarNovoAnexo(Anexo anexo);

    Task<Lancamento?> ObterPorIdAsync(Guid id);

    /// <summary>
    /// Todos os lançamentos da empresa (com anexos) numa única consulta. Por padrão não traz os
    /// excluídos (exclusão lógica) — só quando <paramref name="incluirExcluidos"/> é true.
    /// Somente leitura: as entidades voltam sem rastreamento do EF Core, então alterar uma delas
    /// não é persistido — para alterar, obter via <see cref="ObterPorIdAsync"/>.
    /// </summary>
    Task<List<Lancamento>> ListarPorEmpresaSomenteLeituraAsync(Guid empresaId, bool incluirExcluidos = false);

    void Remover(Lancamento lancamento);

    /// <summary>
    /// Registra um evento novo de histórico/auditoria. Assim como <see cref="RegistrarNovoAnexo"/>,
    /// precisa ser adicionado explicitamente (não é filho de nenhuma coleção de navegação rastreada).
    /// </summary>
    void RegistrarHistorico(HistoricoLancamento historico);

    Task<List<HistoricoLancamento>> ListarHistoricoPorLancamentoAsync(Guid lancamentoId);

    Task<List<HistoricoLancamento>> ListarHistoricoPorPeriodoAsync(Guid empresaId, DateTime inicio, DateTime fim);

    /// <summary>
    /// Inclui lançamentos excluídos (exclusão lógica) na verificação — a linha continua existindo
    /// no banco, e o FK de Lancamento para Conta é Restrict, então mesmo um lançamento excluído
    /// impede a exclusão física da conta.
    /// </summary>
    Task<bool> ExisteLancamentoParaContaAsync(Guid contaId);

    /// <summary>Mesma lógica de <see cref="ExisteLancamentoParaContaAsync"/>, mas para Categoria.</summary>
    Task<bool> ExisteLancamentoParaCategoriaAsync(Guid categoriaId);
}
