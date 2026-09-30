using LivroCaixa.Application.DTOs;
using LivroCaixa.Application.Services;
using LivroCaixa.Domain.Enums;

namespace LivroCaixa.App.ViewModels;

public class LancamentoDetalheViewModel
{
    private readonly AnexoService _anexoService;

    public Guid LancamentoId { get; }

    public LancamentoDetalheExibicaoDto Detalhe { get; }

    public IReadOnlyList<ItemHistoricoExibicao> Historico { get; }

    public bool TemHistorico => Historico.Count > 0;

    // O banco guarda em UTC; a tela mostra o horário local (antes aparecia 3 horas adiantado).
    public DateTime CriadoEmLocal => Detalhe.CriadoEm.ToLocalTime();

    public DateTime? AlteradoEmLocal => Detalhe.AlteradoEm?.ToLocalTime();

    public LancamentoDetalheViewModel(
        AnexoService anexoService,
        Guid lancamentoId,
        LancamentoDetalheExibicaoDto detalhe,
        IReadOnlyList<HistoricoLancamentoDto> historico)
    {
        _anexoService = anexoService;
        LancamentoId = lancamentoId;
        Detalhe = detalhe;
        Historico = historico.Select((evento, indice) => ParaExibicao(evento, ehUltimo: indice == historico.Count - 1)).ToList();
    }

    public Task<string> ObterCaminhoCompletoAnexoAsync(Guid anexoId) =>
        _anexoService.ObterCaminhoCompletoAsync(LancamentoId, anexoId);

    private static ItemHistoricoExibicao ParaExibicao(HistoricoLancamentoDto evento, bool ehUltimo)
    {
        var (titulo, explicacao) = evento.TipoEvento switch
        {
            TipoEventoHistorico.Criado => ("Criado", "Lançamento registrado."),
            // Edição sem campos na lista: salvo sem mudanças, ou mudança num campo que retratos
            // antigos (anteriores a 2026-09-29) não guardavam, como conta e categoria.
            TipoEventoHistorico.Editado => ("Editado", "Nenhuma alteração nos campos registrados."),
            TipoEventoHistorico.Excluido => ("Excluído", "Removido do extrato. Pode ser restaurado em \"Mostrar excluídos\"."),
            TipoEventoHistorico.Restaurado => ("Restaurado", "Voltou para o extrato."),
            _ => (evento.TipoEvento.ToString(), string.Empty)
        };

        return new ItemHistoricoExibicao(
            evento.TipoEvento, titulo, evento.DataHora.ToLocalTime(), explicacao, evento.Alteracoes, ehUltimo);
    }
}
