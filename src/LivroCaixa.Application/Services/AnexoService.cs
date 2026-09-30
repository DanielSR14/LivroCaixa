using LivroCaixa.Application.DTOs;
using LivroCaixa.Application.Interfaces;
using LivroCaixa.Domain.Entities;
using LivroCaixa.Domain.Enums;
using LivroCaixa.Domain.Exceptions;

namespace LivroCaixa.Application.Services;

/// <summary>
/// Comprovantes de um lançamento: o arquivo físico fica em disco (IArmazenamentoAnexoService),
/// e o registro no banco só guarda o caminho relativo, nome original e tamanho.
/// </summary>
public class AnexoService
{
    private readonly ILancamentoRepository _lancamentoRepository;
    private readonly IArmazenamentoAnexoService _armazenamento;
    private readonly IUnitOfWork _unitOfWork;

    public AnexoService(
        ILancamentoRepository lancamentoRepository,
        IArmazenamentoAnexoService armazenamento,
        IUnitOfWork unitOfWork)
    {
        _lancamentoRepository = lancamentoRepository;
        _armazenamento = armazenamento;
        _unitOfWork = unitOfWork;
    }

    public async Task<Anexo> AdicionarAsync(Guid lancamentoId, string caminhoOrigemArquivo)
    {
        var lancamento = await ObterLancamentoOuFalharAsync(lancamentoId);

        var (caminhoArmazenado, tamanhoBytes) = await _armazenamento.SalvarAsync(lancamentoId, caminhoOrigemArquivo);
        var nomeOriginal = Path.GetFileName(caminhoOrigemArquivo);
        var tipoArquivo = InferirTipoArquivo(nomeOriginal);

        var anexo = new Anexo(lancamentoId, caminhoArmazenado, nomeOriginal, tipoArquivo, tamanhoBytes);
        lancamento.AdicionarAnexo(anexo);
        _lancamentoRepository.RegistrarNovoAnexo(anexo);

        await _unitOfWork.SalvarAlteracoesAsync();

        return anexo;
    }

    public async Task RemoverAsync(Guid lancamentoId, Guid anexoId)
    {
        var lancamento = await ObterLancamentoOuFalharAsync(lancamentoId);
        var anexo = lancamento.Anexos.SingleOrDefault(a => a.Id == anexoId)
            ?? throw new DomainException("Anexo não encontrado.");

        lancamento.RemoverAnexo(anexoId);
        await _unitOfWork.SalvarAlteracoesAsync();

        _armazenamento.Remover(anexo.CaminhoArquivo);
    }

    public async Task<IReadOnlyList<AnexoResumoDto>> ListarAsync(Guid lancamentoId)
    {
        var lancamento = await ObterLancamentoOuFalharAsync(lancamentoId);
        return lancamento.Anexos
            .Select(a => new AnexoResumoDto(a.Id, a.NomeOriginal, a.TamanhoBytes))
            .ToList();
    }

    public string ObterCaminhoCompleto(string caminhoArmazenado) => _armazenamento.ObterCaminhoCompleto(caminhoArmazenado);

    public async Task<string> ObterCaminhoCompletoAsync(Guid lancamentoId, Guid anexoId)
    {
        var lancamento = await ObterLancamentoOuFalharAsync(lancamentoId);
        var anexo = lancamento.Anexos.SingleOrDefault(a => a.Id == anexoId)
            ?? throw new DomainException("Anexo não encontrado.");

        return _armazenamento.ObterCaminhoCompleto(anexo.CaminhoArquivo);
    }

    private async Task<Lancamento> ObterLancamentoOuFalharAsync(Guid lancamentoId) =>
        await _lancamentoRepository.ObterPorIdAsync(lancamentoId) ?? throw new DomainException("Lançamento não encontrado.");

    private static TipoArquivoAnexo InferirTipoArquivo(string nomeArquivo)
    {
        var extensao = Path.GetExtension(nomeArquivo).TrimStart('.').ToLowerInvariant();
        return extensao switch
        {
            "pdf" => TipoArquivoAnexo.Pdf,
            "jpg" => TipoArquivoAnexo.Jpg,
            "jpeg" => TipoArquivoAnexo.Jpeg,
            "png" => TipoArquivoAnexo.Png,
            _ => throw new DomainException("Tipo de arquivo não suportado. Use PDF, JPG ou PNG.")
        };
    }
}
