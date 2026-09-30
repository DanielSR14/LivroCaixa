using LivroCaixa.Application.DTOs;
using LivroCaixa.Application.Interfaces;
using LivroCaixa.Domain.Entities;
using LivroCaixa.Domain.Exceptions;

namespace LivroCaixa.Application.Services;

/// <summary>Cadastro dos modelos de lançamento (atalhos por código para lançamentos que se repetem).</summary>
public class ModeloLancamentoService
{
    private readonly IModeloLancamentoRepository _modeloRepository;
    private readonly ICategoriaRepository _categoriaRepository;
    private readonly IContaRepository _contaRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ModeloLancamentoService(
        IModeloLancamentoRepository modeloRepository,
        ICategoriaRepository categoriaRepository,
        IContaRepository contaRepository,
        IUnitOfWork unitOfWork)
    {
        _modeloRepository = modeloRepository;
        _categoriaRepository = categoriaRepository;
        _contaRepository = contaRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<ModeloLancamentoDto>> ListarAsync(Guid empresaId)
    {
        var modelos = await _modeloRepository.ListarPorEmpresaAsync(empresaId);
        var nomesCategorias = (await _categoriaRepository.ListarPorEmpresaAsync(empresaId)).ToDictionary(c => c.Id, c => c.Nome);
        var nomesContas = (await _contaRepository.ListarPorEmpresaAsync(empresaId)).ToDictionary(c => c.Id, c => c.Nome);

        return modelos
            .OrderBy(m => m.Codigo)
            .Select(m => new ModeloLancamentoDto(
                m.Id, m.Codigo, m.Nome, m.Tipo, m.CategoriaId, nomesCategorias.GetValueOrDefault(m.CategoriaId, "—"),
                m.ContaId, m.ContaId is Guid contaId ? nomesContas.GetValueOrDefault(contaId) : null,
                m.FormaPagamento, m.Valor, m.Descricao))
            .ToList();
    }

    public async Task<ModeloLancamento> CriarAsync(Guid empresaId, SalvarModeloLancamentoDto dto)
    {
        await GarantirCodigoDisponivelAsync(empresaId, dto.Codigo, idIgnorado: null);
        var (categoria, conta) = await ObterVinculosAsync(dto);

        var modelo = new ModeloLancamento(
            empresaId, dto.Codigo, dto.Nome, categoria, conta, dto.FormaPagamento, dto.Valor, dto.Descricao);
        await _modeloRepository.AdicionarAsync(modelo);
        await _unitOfWork.SalvarAlteracoesAsync();

        return modelo;
    }

    public async Task AtualizarAsync(Guid modeloId, SalvarModeloLancamentoDto dto)
    {
        var modelo = await ObterModeloOuFalharAsync(modeloId);
        await GarantirCodigoDisponivelAsync(modelo.EmpresaId, dto.Codigo, idIgnorado: modeloId);
        var (categoria, conta) = await ObterVinculosAsync(dto);

        modelo.Atualizar(dto.Codigo, dto.Nome, categoria, conta, dto.FormaPagamento, dto.Valor, dto.Descricao);
        await _unitOfWork.SalvarAlteracoesAsync();
    }

    public async Task ExcluirAsync(Guid modeloId)
    {
        var modelo = await ObterModeloOuFalharAsync(modeloId);
        _modeloRepository.Remover(modelo);
        await _unitOfWork.SalvarAlteracoesAsync();
    }

    // Checado aqui (além do índice único no banco) para dar uma mensagem clara em vez de erro de banco.
    private async Task GarantirCodigoDisponivelAsync(Guid empresaId, string codigo, Guid? idIgnorado)
    {
        var codigoNormalizado = ModeloLancamento.NormalizarCodigo(codigo);
        var existentes = await _modeloRepository.ListarPorEmpresaAsync(empresaId);
        if (existentes.Any(m => m.Id != idIgnorado && m.Codigo == codigoNormalizado))
            throw new DomainException($"Já existe um modelo com o código \"{codigoNormalizado}\".");
    }

    private async Task<(Categoria Categoria, Conta? Conta)> ObterVinculosAsync(SalvarModeloLancamentoDto dto)
    {
        var categoria = await _categoriaRepository.ObterPorIdAsync(dto.CategoriaId)
            ?? throw new DomainException("Categoria não encontrada.");

        Conta? conta = null;
        if (dto.ContaId is Guid contaId)
            conta = await _contaRepository.ObterPorIdAsync(contaId) ?? throw new DomainException("Conta não encontrada.");

        return (categoria, conta);
    }

    private async Task<ModeloLancamento> ObterModeloOuFalharAsync(Guid modeloId) =>
        await _modeloRepository.ObterPorIdAsync(modeloId) ?? throw new DomainException("Modelo não encontrado.");
}
