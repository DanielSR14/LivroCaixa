using LivroCaixa.Domain.Enums;
using LivroCaixa.Domain.Exceptions;

namespace LivroCaixa.Domain.Entities;

/// <summary>
/// Retrato de um lançamento antes de uma edição/exclusão, para permitir auditoria sem
/// precisar reconstruir "a versão anterior" a partir de campos já sobrescritos.
/// </summary>
public class HistoricoLancamento : EntidadeBase
{
    public Guid EmpresaId { get; private set; }
    public Guid LancamentoId { get; private set; }
    public TipoEventoHistorico TipoEvento { get; private set; }
    public DateTime DataHora { get; private set; }

    /// <summary>Retrato em JSON dos campos do lançamento antes da mudança. Nulo para "Criado" e "Restaurado".</summary>
    public string? DadosAnteriores { get; private set; }

    private HistoricoLancamento()
    {
    }

    public HistoricoLancamento(Guid empresaId, Guid lancamentoId, TipoEventoHistorico tipoEvento, string? dadosAnteriores)
    {
        if (empresaId == Guid.Empty)
            throw new DomainException("O histórico precisa estar vinculado a uma empresa.");

        if (lancamentoId == Guid.Empty)
            throw new DomainException("O histórico precisa estar vinculado a um lançamento.");

        EmpresaId = empresaId;
        LancamentoId = lancamentoId;
        TipoEvento = tipoEvento;
        DadosAnteriores = dadosAnteriores;
        DataHora = DateTime.UtcNow;
    }
}
