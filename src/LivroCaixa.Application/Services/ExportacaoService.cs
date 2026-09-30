using System.Globalization;
using LivroCaixa.Application.DTOs.Exportacao;
using LivroCaixa.Application.Interfaces;
using LivroCaixa.Domain.Entities;
using LivroCaixa.Domain.Exceptions;

namespace LivroCaixa.Application.Services;

/// <summary>
/// Monta o pacote .contabil de uma competência (lançamentos, comprovantes e histórico de
/// auditoria) para envio ao escritório, e registra a RemessaExportacao correspondente.
/// </summary>
public class ExportacaoService
{
    private readonly IEmpresaRepository _empresaRepository;
    private readonly IContaRepository _contaRepository;
    private readonly ICategoriaRepository _categoriaRepository;
    private readonly ILancamentoRepository _lancamentoRepository;
    private readonly IArmazenamentoAnexoService _armazenamentoAnexo;
    private readonly IExportadorArquivoService _exportadorArquivo;
    private readonly IRemessaExportacaoRepository _remessaExportacaoRepository;
    private readonly ITransferenciaRepository _transferenciaRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ExportacaoService(
        IEmpresaRepository empresaRepository,
        IContaRepository contaRepository,
        ICategoriaRepository categoriaRepository,
        ILancamentoRepository lancamentoRepository,
        IArmazenamentoAnexoService armazenamentoAnexo,
        IExportadorArquivoService exportadorArquivo,
        IRemessaExportacaoRepository remessaExportacaoRepository,
        ITransferenciaRepository transferenciaRepository,
        IUnitOfWork unitOfWork)
    {
        _empresaRepository = empresaRepository;
        _contaRepository = contaRepository;
        _categoriaRepository = categoriaRepository;
        _lancamentoRepository = lancamentoRepository;
        _armazenamentoAnexo = armazenamentoAnexo;
        _exportadorArquivo = exportadorArquivo;
        _remessaExportacaoRepository = remessaExportacaoRepository;
        _transferenciaRepository = transferenciaRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<string> ExportarAsync(Guid empresaId, int ano, int mes, string diretorioDestino)
    {
        var empresa = await _empresaRepository.ObterUnicaAsync()
            ?? throw new DomainException("Empresa não encontrada.");

        var contas = await _contaRepository.ListarPorEmpresaAsync(empresaId);
        if (contas.Count == 0)
            throw new DomainException("Nenhuma conta cadastrada para esta empresa.");

        var categoriasPorId = (await _categoriaRepository.ListarPorEmpresaAsync(empresaId))
            .ToDictionary(c => c.Id);
        var contasPorId = contas.ToDictionary(c => c.Id);

        var lancamentosDoPeriodo = (await _lancamentoRepository.ListarPorEmpresaSomenteLeituraAsync(empresaId))
            .Where(l => contasPorId.ContainsKey(l.ContaId) && l.Data.Year == ano && l.Data.Month == mes)
            .OrderBy(l => l.Data)
            .ThenBy(l => l.CriadoEm)
            .ToList();

        // Histórico de auditoria por data da ALTERAÇÃO (não da competência do lançamento em si):
        // uma correção feita em setembro num lançamento de agosto entra na exportação de
        // setembro, não na de agosto (que já foi enviada e fechada) — ver IDEIAS_E_MELHORIAS.md, item 5.
        var inicioDoPeriodo = new DateTime(ano, mes, 1, 0, 0, 0, DateTimeKind.Utc);
        var fimDoPeriodo = inicioDoPeriodo.AddMonths(1).AddTicks(-1);
        var eventosHistorico = await _lancamentoRepository.ListarHistoricoPorPeriodoAsync(empresaId, inicioDoPeriodo, fimDoPeriodo);

        var transferenciasExportacao = (await _transferenciaRepository.ListarPorEmpresaSomenteLeituraAsync(empresaId))
            .Where(t => t.Data.Year == ano && t.Data.Month == mes)
            .OrderBy(t => t.Data)
            .ThenBy(t => t.CriadoEm)
            .Select(t => new TransferenciaExportacaoDto(
                FormatarData(t.Data),
                contasPorId.TryGetValue(t.ContaOrigemId, out var origem) ? origem.Nome : "—",
                contasPorId.TryGetValue(t.ContaDestinoId, out var destino) ? destino.Nome : "—",
                t.Valor,
                t.Descricao))
            .ToList();

        if (lancamentosDoPeriodo.Count == 0 && eventosHistorico.Count == 0 && transferenciasExportacao.Count == 0)
            throw new DomainException("Não há lançamentos nem alterações nesse período para exportar.");

        var lancamentosExportacao = new List<LancamentoExportacaoDto>();
        var instrucoesCopia = new List<InstrucaoCopiaAnexoDto>();
        var contadorAnexo = 1;

        foreach (var lancamento in lancamentosDoPeriodo)
        {
            var nomeCategoria = categoriasPorId.TryGetValue(lancamento.CategoriaId, out var categoria)
                ? categoria.Nome
                : "—";
            var nomeConta = contasPorId.TryGetValue(lancamento.ContaId, out var contaDoLancamento)
                ? contaDoLancamento.Nome
                : "—";

            var nomesAnexos = new List<string>();
            // Só comprovantes cujo arquivo ainda existe: um já apagado pela limpeza de Armazenamento
            // ficaria citado no JSON sem estar no zip, e a ferramenta do escritório procuraria por ele.
            foreach (var anexo in lancamento.Anexos.Where(a => _armazenamentoAnexo.Existe(a.CaminhoArquivo)))
            {
                var extensao = Path.GetExtension(anexo.NomeOriginal);
                var nomeDestino = $"{contadorAnexo:D6}{extensao}";
                contadorAnexo++;

                nomesAnexos.Add(nomeDestino);
                instrucoesCopia.Add(new InstrucaoCopiaAnexoDto(
                    _armazenamentoAnexo.ObterCaminhoCompleto(anexo.CaminhoArquivo), nomeDestino));
            }

            lancamentosExportacao.Add(new LancamentoExportacaoDto(
                FormatarData(lancamento.Data),
                lancamento.Tipo.ToString(),
                lancamento.Valor,
                nomeCategoria,
                nomeConta,
                lancamento.FormaPagamento.ToString(),
                lancamento.Descricao,
                lancamento.Observacoes,
                nomesAnexos));
        }

        var historicoExportacao = eventosHistorico
            .OrderBy(e => e.DataHora)
            .Select(e => new HistoricoExportacaoDto(e.LancamentoId.ToString(), e.TipoEvento.ToString(), e.DataHora, e.DadosAnteriores))
            .ToList();

        var periodo = $"{ano:D4}-{mes:D2}";
        // 1.2: inclui transferencias.json (transferências entre contas, fora de lancamentos.json).
        var manifesto = new ManifestoExportacaoDto("1.2", empresa.Cnpj, periodo, DateTime.UtcNow, lancamentosExportacao.Count);
        var empresaExportacao = new EmpresaExportacaoDto(empresa.RazaoSocial, empresa.NomeFantasia, empresa.Cnpj, empresa.Segmento);
        var pacote = new PacoteExportacaoDto(empresa.Cnpj, ano, mes, manifesto, empresaExportacao, lancamentosExportacao, historicoExportacao, transferenciasExportacao);

        var caminhoGerado = await _exportadorArquivo.GerarArquivoAsync(pacote, instrucoesCopia, diretorioDestino);

        // RemessaExportacao só faz sentido quando há lançamentos da competência (é ela que autoriza a
        // limpeza de comprovantes na tela de Armazenamento); uma exportação só com histórico de auditoria
        // de lançamentos de outras competências não cria remessa.
        if (lancamentosExportacao.Count > 0)
        {
            var remessa = new RemessaExportacao(empresaId, ano, mes, lancamentosExportacao.Count, caminhoGerado);
            await _remessaExportacaoRepository.AdicionarAsync(remessa);
            await _unitOfWork.SalvarAlteracoesAsync();
        }

        return caminhoGerado;
    }

    // Invariante de propósito: na cultura do Windows, "yyyy" segue o calendário local (ex: th-TH
    // usa o budista e daria 2569) — e o arquivo é lido por outro programa, não por uma pessoa.
    private static string FormatarData(DateOnly data) => data.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
}
