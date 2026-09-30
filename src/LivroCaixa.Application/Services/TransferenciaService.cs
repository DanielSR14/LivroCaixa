using LivroCaixa.Application.DTOs;
using LivroCaixa.Application.Interfaces;
using LivroCaixa.Domain.Entities;
using LivroCaixa.Domain.Exceptions;

namespace LivroCaixa.Application.Services;

/// <summary>Registro e exclusão de transferências entre contas (a leitura fica no extrato do LancamentoService).</summary>
public class TransferenciaService
{
    private readonly ITransferenciaRepository _transferenciaRepository;
    private readonly IContaRepository _contaRepository;
    private readonly IUnitOfWork _unitOfWork;

    public TransferenciaService(
        ITransferenciaRepository transferenciaRepository,
        IContaRepository contaRepository,
        IUnitOfWork unitOfWork)
    {
        _transferenciaRepository = transferenciaRepository;
        _contaRepository = contaRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Transferencia> RegistrarAsync(Guid empresaId, NovaTransferenciaDto dto)
    {
        var origem = await ObterContaOuFalharAsync(dto.ContaOrigemId);
        var destino = await ObterContaOuFalharAsync(dto.ContaDestinoId);

        var transferencia = new Transferencia(empresaId, origem, destino, dto.Data, dto.Valor, dto.Descricao);
        await _transferenciaRepository.AdicionarAsync(transferencia);
        await _unitOfWork.SalvarAlteracoesAsync();

        return transferencia;
    }

    public async Task ExcluirAsync(Guid transferenciaId)
    {
        var transferencia = await _transferenciaRepository.ObterPorIdAsync(transferenciaId)
            ?? throw new DomainException("Transferência não encontrada.");

        transferencia.Excluir();
        await _unitOfWork.SalvarAlteracoesAsync();
    }

    private async Task<Conta> ObterContaOuFalharAsync(Guid contaId) =>
        await _contaRepository.ObterPorIdAsync(contaId) ?? throw new DomainException("Conta não encontrada.");
}
