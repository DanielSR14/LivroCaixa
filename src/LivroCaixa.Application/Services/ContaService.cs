using LivroCaixa.Application.DTOs;
using LivroCaixa.Application.Interfaces;
using LivroCaixa.Domain.Entities;
using LivroCaixa.Domain.Exceptions;
using LivroCaixa.Domain.Rules;

namespace LivroCaixa.Application.Services;

/// <summary>
/// Cadastro de contas (caixa, banco, cartão, investimento). Edição de saldo inicial e exclusão
/// são bloqueadas quando a conta já tem lançamentos, com mensagem amigável em vez do erro de FK.
/// </summary>
public class ContaService
{
    private readonly IContaRepository _contaRepository;
    private readonly ILancamentoRepository _lancamentoRepository;
    private readonly ITransferenciaRepository _transferenciaRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ContaService(
        IContaRepository contaRepository,
        ILancamentoRepository lancamentoRepository,
        ITransferenciaRepository transferenciaRepository,
        IUnitOfWork unitOfWork)
    {
        _contaRepository = contaRepository;
        _lancamentoRepository = lancamentoRepository;
        _transferenciaRepository = transferenciaRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Conta> CriarContaAsync(Guid empresaId, NovaContaDto dto)
    {
        var conta = new Conta(
            empresaId, dto.Nome, dto.Tipo, dto.SaldoInicial,
            dto.Agencia, dto.NumeroConta, dto.TipoContaBancaria);
        await _contaRepository.AdicionarAsync(conta);
        await _unitOfWork.SalvarAlteracoesAsync();

        return conta;
    }

    public async Task<IReadOnlyList<ContaResumoDto>> ListarComSaldoAsync(Guid empresaId)
    {
        var contas = await _contaRepository.ListarPorEmpresaAsync(empresaId);
        var lancamentosPorConta = (await _lancamentoRepository.ListarPorEmpresaSomenteLeituraAsync(empresaId))
            .ToLookup(l => l.ContaId);
        var transferencias = await _transferenciaRepository.ListarPorEmpresaSomenteLeituraAsync(empresaId);

        return contas
            .Select(conta => new ContaResumoDto(
                conta.Id, conta.Nome, conta.Tipo, conta.SaldoInicial,
                CalculadoraSaldo.CalcularSaldoAtual(conta, lancamentosPorConta[conta.Id], transferencias),
                conta.Agencia, conta.NumeroConta, conta.TipoContaBancaria))
            .ToList();
    }

    public async Task AtualizarContaAsync(AtualizarContaDto dto)
    {
        var conta = await ObterContaOuFalharAsync(dto.ContaId);

        if (dto.SaldoInicial != conta.SaldoInicial
            && (await _lancamentoRepository.ExisteLancamentoParaContaAsync(dto.ContaId)
                || await _transferenciaRepository.ExisteTransferenciaParaContaAsync(dto.ContaId)))
            throw new DomainException("Não é possível alterar o saldo inicial de uma conta que já tem movimentações.");

        conta.Atualizar(dto.Nome, dto.Tipo, dto.SaldoInicial, dto.Agencia, dto.NumeroConta, dto.TipoContaBancaria);
        await _unitOfWork.SalvarAlteracoesAsync();
    }

    public async Task ExcluirContaAsync(Guid contaId)
    {
        var conta = await ObterContaOuFalharAsync(contaId);

        if (await _lancamentoRepository.ExisteLancamentoParaContaAsync(contaId))
            throw new DomainException("Não é possível excluir uma conta que já tem lançamentos.");

        if (await _transferenciaRepository.ExisteTransferenciaParaContaAsync(contaId))
            throw new DomainException("Não é possível excluir uma conta que já tem transferências.");

        _contaRepository.Remover(conta);
        await _unitOfWork.SalvarAlteracoesAsync();
    }

    private async Task<Conta> ObterContaOuFalharAsync(Guid contaId) =>
        await _contaRepository.ObterPorIdAsync(contaId) ?? throw new DomainException("Conta não encontrada.");
}
