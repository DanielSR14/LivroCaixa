using LivroCaixa.Application.DTOs;
using LivroCaixa.Application.Interfaces;

namespace LivroCaixa.Application.Services;

/// <summary>
/// O que ainda falta para o escritório receber tudo completo e em dia — mostrado no Dashboard.
/// Só olha competências ainda não exportadas: o que já foi enviado não é mais pendência do cliente.
/// </summary>
public class PendenciasService
{
    private readonly ILancamentoRepository _lancamentoRepository;
    private readonly IRemessaExportacaoRepository _remessaExportacaoRepository;

    public PendenciasService(ILancamentoRepository lancamentoRepository, IRemessaExportacaoRepository remessaExportacaoRepository)
    {
        _lancamentoRepository = lancamentoRepository;
        _remessaExportacaoRepository = remessaExportacaoRepository;
    }

    public async Task<PendenciasDto> ObterAsync(Guid empresaId, DateTime hoje)
    {
        var exportadas = (await _remessaExportacaoRepository.ListarPorEmpresaAsync(empresaId))
            .Select(r => (r.Ano, r.Mes))
            .ToHashSet();

        var aindaNaoEnviados = (await _lancamentoRepository.ListarPorEmpresaSomenteLeituraAsync(empresaId))
            .Where(l => !exportadas.Contains((l.Data.Year, l.Data.Month)))
            .ToList();

        var semComprovante = aindaNaoEnviados.Count(l => l.Anexos.Count == 0);

        // O mês atual ainda está em andamento — só vira pendência de exportação quando fechar.
        var mesAtual = (hoje.Year, hoje.Month);
        var competenciasNaoExportadas = aindaNaoEnviados
            .Select(l => (Ano: l.Data.Year, Mes: l.Data.Month))
            .Distinct()
            .Where(c => c.Ano < mesAtual.Year || (c.Ano == mesAtual.Year && c.Mes < mesAtual.Month))
            .OrderBy(c => c.Ano)
            .ThenBy(c => c.Mes)
            .ToList();

        return new PendenciasDto(semComprovante, competenciasNaoExportadas);
    }
}
