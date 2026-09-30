using LivroCaixa.Application.DTOs;
using LivroCaixa.Application.Interfaces;
using LivroCaixa.Domain.Entities;
using LivroCaixa.Domain.Enums;
using LivroCaixa.Domain.Exceptions;

namespace LivroCaixa.Application.Services;

/// <summary>
/// Casos de uso de lançamentos: cadastro/edição/exclusão lógica/restauração (cada um grava um
/// evento em HistoricoLancamento para auditoria) e as leituras do extrato consolidado (que também
/// mostra as transferências entre contas, já que elas mexem no saldo de cada conta).
/// </summary>
public class LancamentoService
{
    /// <summary>"Categoria" exibida nas linhas de transferência do extrato (não é uma Categoria de verdade).</summary>
    public const string NomeCategoriaTransferencia = "Transferência";

    private readonly ILancamentoRepository _lancamentoRepository;
    private readonly IContaRepository _contaRepository;
    private readonly ICategoriaRepository _categoriaRepository;
    private readonly ITransferenciaRepository _transferenciaRepository;
    private readonly IUnitOfWork _unitOfWork;

    public LancamentoService(
        ILancamentoRepository lancamentoRepository,
        IContaRepository contaRepository,
        ICategoriaRepository categoriaRepository,
        ITransferenciaRepository transferenciaRepository,
        IUnitOfWork unitOfWork)
    {
        _lancamentoRepository = lancamentoRepository;
        _contaRepository = contaRepository;
        _categoriaRepository = categoriaRepository;
        _transferenciaRepository = transferenciaRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Lancamento> RegistrarAsync(Guid empresaId, NovoLancamentoDto dto)
    {
        var conta = await ObterContaOuFalharAsync(dto.ContaId);
        var categoria = await ObterCategoriaOuFalharAsync(dto.CategoriaId);

        var lancamento = new Lancamento(
            empresaId, conta, categoria, dto.Tipo, dto.Data, dto.Valor, dto.FormaPagamento, dto.Descricao, dto.Observacoes);

        await _lancamentoRepository.AdicionarAsync(lancamento);
        _lancamentoRepository.RegistrarHistorico(
            new HistoricoLancamento(empresaId, lancamento.Id, TipoEventoHistorico.Criado, null));
        await _unitOfWork.SalvarAlteracoesAsync();

        return lancamento;
    }

    public async Task AtualizarAsync(AtualizarLancamentoDto dto)
    {
        var lancamento = await ObterLancamentoOuFalharAsync(dto.LancamentoId);
        var conta = await ObterContaOuFalharAsync(dto.ContaId);
        var categoria = await ObterCategoriaOuFalharAsync(dto.CategoriaId);

        var retratoAnterior = await CriarRetratoAsync(lancamento);
        lancamento.Atualizar(conta, categoria, dto.Data, dto.Valor, dto.FormaPagamento, dto.Descricao, dto.Observacoes);
        _lancamentoRepository.RegistrarHistorico(
            new HistoricoLancamento(lancamento.EmpresaId, lancamento.Id, TipoEventoHistorico.Editado, retratoAnterior));

        await _unitOfWork.SalvarAlteracoesAsync();
    }

    public async Task ExcluirAsync(Guid lancamentoId)
    {
        var lancamento = await ObterLancamentoOuFalharAsync(lancamentoId);
        var retratoAnterior = await CriarRetratoAsync(lancamento);

        lancamento.Excluir();
        _lancamentoRepository.RegistrarHistorico(
            new HistoricoLancamento(lancamento.EmpresaId, lancamento.Id, TipoEventoHistorico.Excluido, retratoAnterior));

        await _unitOfWork.SalvarAlteracoesAsync();
    }

    public async Task RestaurarAsync(Guid lancamentoId)
    {
        var lancamento = await ObterLancamentoOuFalharAsync(lancamentoId);

        lancamento.Restaurar();
        _lancamentoRepository.RegistrarHistorico(
            new HistoricoLancamento(lancamento.EmpresaId, lancamento.Id, TipoEventoHistorico.Restaurado, null));

        await _unitOfWork.SalvarAlteracoesAsync();
    }

    public async Task<LancamentoDetalheDto> ObterDetalheAsync(Guid lancamentoId)
    {
        var lancamento = await ObterLancamentoOuFalharAsync(lancamentoId);

        return new LancamentoDetalheDto(
            lancamento.Id, lancamento.ContaId, lancamento.CategoriaId, lancamento.Tipo,
            lancamento.Data, lancamento.Valor, lancamento.FormaPagamento, lancamento.Descricao, lancamento.Observacoes);
    }

    public async Task<LancamentoDetalheExibicaoDto> ObterDetalheExibicaoAsync(Guid lancamentoId)
    {
        var lancamento = await ObterLancamentoOuFalharAsync(lancamentoId);
        var categoria = await _categoriaRepository.ObterPorIdAsync(lancamento.CategoriaId);
        var conta = await _contaRepository.ObterPorIdAsync(lancamento.ContaId);

        var anexos = lancamento.Anexos
            .Select(a => new AnexoResumoDto(a.Id, a.NomeOriginal, a.TamanhoBytes))
            .ToList();

        return new LancamentoDetalheExibicaoDto(
            lancamento.Id, lancamento.Tipo, lancamento.Data, lancamento.Valor,
            categoria?.Nome ?? "—", conta?.Nome ?? "—", conta?.Tipo ?? TipoConta.Caixa, lancamento.FormaPagamento,
            lancamento.Descricao, lancamento.Observacoes, lancamento.CriadoEm, lancamento.AlteradoEm, anexos);
    }

    /// <summary>
    /// Extrato consolidado de todas as contas da empresa, com o saldo de cada linha
    /// calculado dentro da própria conta a que ela pertence (o "SaldoAposLancamento" nunca
    /// mistura saldo de contas diferentes).
    /// </summary>
    public async Task<IReadOnlyList<LancamentoResumoDto>> ListarExtratoConsolidadoAsync(Guid empresaId) =>
        (await ObterExtratoConsolidadoAsync(empresaId)).Lancamentos;

    /// <summary>
    /// Mesmo extrato de <see cref="ListarExtratoConsolidadoAsync"/> mais o saldo total, numa
    /// única leitura do banco — é o que o Dashboard usa ao abrir/recarregar.
    /// </summary>
    public async Task<ExtratoConsolidadoDto> ObterExtratoConsolidadoAsync(Guid empresaId)
    {
        var contas = await _contaRepository.ListarPorEmpresaAsync(empresaId);
        var nomesCategorias = await ObterNomesCategoriasAsync(empresaId);
        var nomesContas = contas.ToDictionary(c => c.Id, c => c.Nome);
        var lancamentosPorConta = (await _lancamentoRepository.ListarPorEmpresaSomenteLeituraAsync(empresaId))
            .ToLookup(l => l.ContaId);
        var transferencias = await _transferenciaRepository.ListarPorEmpresaSomenteLeituraAsync(empresaId);
        // Cada transferência aparece duas vezes: saindo da origem e entrando no destino.
        var transferenciasPorConta = transferencias
            .SelectMany(t => new[] { (ContaId: t.ContaOrigemId, Transferencia: t), (ContaId: t.ContaDestinoId, Transferencia: t) })
            .ToLookup(x => x.ContaId, x => x.Transferencia);

        var resultado = new List<LancamentoResumoDto>();
        var saldoTotal = 0m;

        foreach (var conta in contas)
        {
            // Lançamentos e transferências da conta em ordem cronológica única, para o saldo linha a
            // linha ficar certo; cada item sabe criar a própria linha a partir do saldo acumulado.
            var movimentos = lancamentosPorConta[conta.Id]
                .Select(l => new MovimentoConta(l.Data, l.CriadoEm, l.Tipo == TipoLancamento.Entrada ? l.Valor : -l.Valor,
                    saldo => CriarResumo(l, conta, nomesCategorias, saldo, excluido: false)))
                .Concat(transferenciasPorConta[conta.Id]
                    .Select(t => new MovimentoConta(t.Data, t.CriadoEm, t.EfeitoNoSaldo(conta.Id),
                        saldo => CriarResumoTransferencia(t, conta, nomesContas, saldo))))
                .OrderBy(m => m.Data)
                .ThenBy(m => m.CriadoEm);

            var saldo = conta.SaldoInicial;
            foreach (var movimento in movimentos)
            {
                saldo += movimento.EfeitoNoSaldo;
                resultado.Add(movimento.CriarLinha(saldo));
            }

            saldoTotal += saldo;
        }

        return new ExtratoConsolidadoDto(resultado.OrderByDescending(r => r.Data).ToList(), saldoTotal);
    }

    private sealed record MovimentoConta(
        DateOnly Data, DateTime CriadoEm, decimal EfeitoNoSaldo, Func<decimal, LancamentoResumoDto> CriarLinha);

    private static LancamentoResumoDto CriarResumoTransferencia(
        Transferencia transferencia, Conta conta, IReadOnlyDictionary<Guid, string> nomesContas, decimal saldoAposLancamento)
    {
        var entrando = transferencia.ContaDestinoId == conta.Id;
        var outraConta = nomesContas.GetValueOrDefault(entrando ? transferencia.ContaOrigemId : transferencia.ContaDestinoId, "—");
        var descricao = entrando ? $"Transferência de {outraConta}" : $"Transferência para {outraConta}";
        if (transferencia.Descricao is not null)
            descricao += $" — {transferencia.Descricao}";

        return new LancamentoResumoDto(
            transferencia.Id, transferencia.Data, descricao, NomeCategoriaTransferencia, conta.Nome, conta.Id, conta.Tipo,
            entrando ? TipoLancamento.Entrada : TipoLancamento.Saida, transferencia.Valor, saldoAposLancamento,
            QuantidadeAnexos: 0, FoiEditado: false, Excluido: false, Observacoes: null, EhTransferencia: true);
    }

    /// <summary>
    /// Lançamentos excluídos (exclusão lógica) de todas as contas da empresa, para a tela de
    /// auditoria — sem saldo acumulado (não faz sentido para um lançamento fora do extrato ativo).
    /// </summary>
    public async Task<IReadOnlyList<LancamentoResumoDto>> ListarExcluidosAsync(Guid empresaId)
    {
        var contasPorId = (await _contaRepository.ListarPorEmpresaAsync(empresaId)).ToDictionary(c => c.Id);
        var nomesCategorias = await ObterNomesCategoriasAsync(empresaId);
        var lancamentos = await _lancamentoRepository.ListarPorEmpresaSomenteLeituraAsync(empresaId, incluirExcluidos: true);

        return lancamentos
            .Where(l => l.ExcluidoEm is not null && contasPorId.ContainsKey(l.ContaId))
            .Select(l => CriarResumo(l, contasPorId[l.ContaId], nomesCategorias, saldoAposLancamento: 0m, excluido: true))
            .OrderByDescending(r => r.Data)
            .ToList();
    }

    /// <summary>Histórico de auditoria (criação/edições/exclusão/restauração) de um lançamento, mais recente primeiro.</summary>
    /// <remarks>
    /// Cada evento só guarda o retrato de ANTES. O "depois" de uma edição é o retrato de antes do
    /// próximo evento que tenha retrato (outra edição ou a exclusão, que não muda campos) — ou, se
    /// ela foi a última, o estado atual do lançamento.
    /// </remarks>
    public async Task<IReadOnlyList<HistoricoLancamentoDto>> ListarHistoricoAsync(Guid lancamentoId)
    {
        var lancamento = await ObterLancamentoOuFalharAsync(lancamentoId);
        var estadoAtual = RetratoLancamento.Ler(await CriarRetratoAsync(lancamento));
        var eventos = (await _lancamentoRepository.ListarHistoricoPorLancamentoAsync(lancamentoId))
            .OrderBy(e => e.DataHora)
            .ToList();

        var resultado = new List<HistoricoLancamentoDto>();
        for (var i = 0; i < eventos.Count; i++)
        {
            var evento = eventos[i];
            IReadOnlyList<AlteracaoCampoDto> alteracoes = Array.Empty<AlteracaoCampoDto>();

            if (evento.TipoEvento == TipoEventoHistorico.Editado)
            {
                var antes = RetratoLancamento.Ler(evento.DadosAnteriores);
                var proximoRetrato = eventos.Skip(i + 1).Select(e => e.DadosAnteriores).FirstOrDefault(d => d is not null);
                var depois = proximoRetrato is null ? estadoAtual : RetratoLancamento.Ler(proximoRetrato);

                if (antes is not null && depois is not null)
                    alteracoes = RetratoLancamento.Comparar(antes, depois);
            }

            resultado.Add(new HistoricoLancamentoDto(evento.TipoEvento, evento.DataHora, evento.DadosAnteriores, alteracoes));
        }

        resultado.Reverse();
        return resultado;
    }

    /// <summary>Retrato com os nomes de conta e categoria da época (renomear/excluir depois não afeta o histórico).</summary>
    private async Task<string> CriarRetratoAsync(Lancamento lancamento)
    {
        var conta = await _contaRepository.ObterPorIdAsync(lancamento.ContaId);
        var categoria = await _categoriaRepository.ObterPorIdAsync(lancamento.CategoriaId);
        return RetratoLancamento.Serializar(lancamento, conta?.Nome, categoria?.Nome);
    }

    /// <summary>Soma do saldo atual de todas as contas da empresa.</summary>
    public async Task<decimal> ObterSaldoTotalAsync(Guid empresaId) =>
        (await ObterExtratoConsolidadoAsync(empresaId)).SaldoTotal;

    private async Task<Dictionary<Guid, string>> ObterNomesCategoriasAsync(Guid empresaId) =>
        (await _categoriaRepository.ListarPorEmpresaAsync(empresaId)).ToDictionary(c => c.Id, c => c.Nome);

    private static LancamentoResumoDto CriarResumo(
        Lancamento lancamento, Conta conta, IReadOnlyDictionary<Guid, string> nomesCategorias,
        decimal saldoAposLancamento, bool excluido) =>
        new(
            lancamento.Id, lancamento.Data, lancamento.Descricao ?? string.Empty,
            nomesCategorias.GetValueOrDefault(lancamento.CategoriaId, "—"), conta.Nome,
            conta.Id, conta.Tipo, lancamento.Tipo, lancamento.Valor, saldoAposLancamento, lancamento.Anexos.Count,
            lancamento.AlteradoEm.HasValue, excluido, lancamento.Observacoes);

    private async Task<Conta> ObterContaOuFalharAsync(Guid contaId) =>
        await _contaRepository.ObterPorIdAsync(contaId) ?? throw new DomainException("Conta não encontrada.");

    private async Task<Categoria> ObterCategoriaOuFalharAsync(Guid categoriaId) =>
        await _categoriaRepository.ObterPorIdAsync(categoriaId) ?? throw new DomainException("Categoria não encontrada.");

    private async Task<Lancamento> ObterLancamentoOuFalharAsync(Guid lancamentoId) =>
        await _lancamentoRepository.ObterPorIdAsync(lancamentoId) ?? throw new DomainException("Lançamento não encontrado.");
}
