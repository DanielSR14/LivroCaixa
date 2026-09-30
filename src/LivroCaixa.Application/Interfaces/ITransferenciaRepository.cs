using LivroCaixa.Domain.Entities;

namespace LivroCaixa.Application.Interfaces;

public interface ITransferenciaRepository
{
    Task AdicionarAsync(Transferencia transferencia);

    Task<Transferencia?> ObterPorIdAsync(Guid id);

    /// <summary>Transferências ativas (não excluídas) da empresa, sem rastreamento do EF Core — só leitura.</summary>
    Task<List<Transferencia>> ListarPorEmpresaSomenteLeituraAsync(Guid empresaId);

    /// <summary>Inclui as excluídas: a linha continua no banco e o FK para Conta é Restrict.</summary>
    Task<bool> ExisteTransferenciaParaContaAsync(Guid contaId);
}
