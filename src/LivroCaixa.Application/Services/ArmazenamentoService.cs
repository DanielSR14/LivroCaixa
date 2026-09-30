using LivroCaixa.Application.DTOs;
using LivroCaixa.Application.Interfaces;
using LivroCaixa.Domain.Entities;
using LivroCaixa.Domain.Exceptions;

namespace LivroCaixa.Application.Services;

/// <summary>
/// Mostra quanto espaço os comprovantes anexados estão ocupando, por competência, e permite
/// apagar o arquivo físico deles com segurança — só de competências já exportadas para o
/// escritório (ver RemessaExportacao), porque o arquivo já está salvo em outro lugar nesse caso.
/// </summary>
public class ArmazenamentoService
{
    private readonly IContaRepository _contaRepository;
    private readonly ILancamentoRepository _lancamentoRepository;
    private readonly IRemessaExportacaoRepository _remessaExportacaoRepository;
    private readonly AnexoService _anexoService;

    public ArmazenamentoService(
        IContaRepository contaRepository,
        ILancamentoRepository lancamentoRepository,
        IRemessaExportacaoRepository remessaExportacaoRepository,
        AnexoService anexoService)
    {
        _contaRepository = contaRepository;
        _lancamentoRepository = lancamentoRepository;
        _remessaExportacaoRepository = remessaExportacaoRepository;
        _anexoService = anexoService;
    }

    public async Task<IReadOnlyList<ResumoCompetenciaDto>> ListarResumoPorCompetenciaAsync(Guid empresaId)
    {
        var lancamentos = await ListarTodosLancamentosAsync(empresaId);
        var competenciasExportadas = await ObterCompetenciasExportadasAsync(empresaId);

        return lancamentos
            .GroupBy(l => (l.Data.Year, l.Data.Month))
            .Select(grupo => new ResumoCompetenciaDto(
                grupo.Key.Year,
                grupo.Key.Month,
                grupo.Count(l => l.ExcluidoEm is null),
                grupo.Sum(l => l.Anexos.Count),
                grupo.Sum(l => l.Anexos.Sum(a => a.TamanhoBytes)),
                competenciasExportadas.Contains((grupo.Key.Year, grupo.Key.Month))))
            .OrderByDescending(r => r.Ano)
            .ThenByDescending(r => r.Mes)
            .ToList();
    }

    /// <summary>
    /// Apaga o arquivo físico de todos os comprovantes dos lançamentos da competência
    /// informada. Retorna a quantidade de comprovantes removidos. Os lançamentos em si
    /// não são afetados, só os anexos.
    /// </summary>
    public async Task<int> LimparAnexosDeCompetenciaAsync(Guid empresaId, int ano, int mes)
    {
        var competenciasExportadas = await ObterCompetenciasExportadasAsync(empresaId);
        if (!competenciasExportadas.Contains((ano, mes)))
            throw new DomainException("Só é possível limpar comprovantes de uma competência que já foi exportada para o escritório.");

        var lancamentos = await ListarTodosLancamentosAsync(empresaId);
        var lancamentosDaCompetencia = lancamentos.Where(l => l.Data.Year == ano && l.Data.Month == mes);

        var quantidadeRemovida = 0;
        foreach (var lancamento in lancamentosDaCompetencia)
        {
            var idsDosAnexos = lancamento.Anexos.Select(a => a.Id).ToList();
            foreach (var anexoId in idsDosAnexos)
            {
                await _anexoService.RemoverAsync(lancamento.Id, anexoId);
                quantidadeRemovida++;
            }
        }

        return quantidadeRemovida;
    }

    /// <summary>
    /// Inclui os excluídos (exclusão lógica): o comprovante de um lançamento excluído continua no
    /// disco, então precisa aparecer no espaço ocupado e ser apagado pela limpeza da competência.
    /// </summary>
    private async Task<List<Lancamento>> ListarTodosLancamentosAsync(Guid empresaId)
    {
        var idsContas = (await _contaRepository.ListarPorEmpresaAsync(empresaId)).Select(c => c.Id).ToHashSet();
        var lancamentos = await _lancamentoRepository.ListarPorEmpresaSomenteLeituraAsync(empresaId, incluirExcluidos: true);
        return lancamentos.Where(l => idsContas.Contains(l.ContaId)).ToList();
    }

    private async Task<HashSet<(int Ano, int Mes)>> ObterCompetenciasExportadasAsync(Guid empresaId)
    {
        var remessas = await _remessaExportacaoRepository.ListarPorEmpresaAsync(empresaId);
        return remessas.Select(r => (r.Ano, r.Mes)).ToHashSet();
    }
}
