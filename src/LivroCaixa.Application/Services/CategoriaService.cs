using LivroCaixa.Application.Interfaces;
using LivroCaixa.Domain.Entities;
using LivroCaixa.Domain.Enums;
using LivroCaixa.Domain.Exceptions;

namespace LivroCaixa.Application.Services;

public class CategoriaService
{
    private readonly ICategoriaRepository _categoriaRepository;
    private readonly ILancamentoRepository _lancamentoRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CategoriaService(
        ICategoriaRepository categoriaRepository,
        ILancamentoRepository lancamentoRepository,
        IUnitOfWork unitOfWork)
    {
        _categoriaRepository = categoriaRepository;
        _lancamentoRepository = lancamentoRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Categoria> CriarAsync(Guid empresaId, string nome, TipoLancamento tipo)
    {
        var categoriasExistentes = await _categoriaRepository.ListarPorEmpresaAsync(empresaId);
        var jaExiste = categoriasExistentes.Any(c =>
            c.Tipo == tipo && string.Equals(c.Nome, nome?.Trim(), StringComparison.OrdinalIgnoreCase));

        if (jaExiste)
            throw new DomainException("Já existe uma categoria com esse nome para esse tipo (entrada/saída).");

        var categoria = new Categoria(empresaId, nome!, tipo);
        await _categoriaRepository.AdicionarVariasAsync(new[] { categoria });
        await _unitOfWork.SalvarAlteracoesAsync();

        return categoria;
    }

    public Task<List<Categoria>> ListarAsync(Guid empresaId) => _categoriaRepository.ListarPorEmpresaAsync(empresaId);

    public async Task AtualizarAsync(Guid categoriaId, string nome)
    {
        var categoria = await ObterCategoriaOuFalharAsync(categoriaId);

        var categoriasExistentes = await _categoriaRepository.ListarPorEmpresaAsync(categoria.EmpresaId);
        var jaExiste = categoriasExistentes.Any(c =>
            c.Id != categoriaId && c.Tipo == categoria.Tipo &&
            string.Equals(c.Nome, nome?.Trim(), StringComparison.OrdinalIgnoreCase));

        if (jaExiste)
            throw new DomainException("Já existe uma categoria com esse nome para esse tipo (entrada/saída).");

        categoria.Atualizar(nome!);
        await _unitOfWork.SalvarAlteracoesAsync();
    }

    public async Task ExcluirAsync(Guid categoriaId)
    {
        var categoria = await ObterCategoriaOuFalharAsync(categoriaId);

        if (await _lancamentoRepository.ExisteLancamentoParaCategoriaAsync(categoriaId))
            throw new DomainException("Não é possível excluir uma categoria que já tem lançamentos.");

        _categoriaRepository.Remover(categoria);
        await _unitOfWork.SalvarAlteracoesAsync();
    }

    private async Task<Categoria> ObterCategoriaOuFalharAsync(Guid categoriaId) =>
        await _categoriaRepository.ObterPorIdAsync(categoriaId) ?? throw new DomainException("Categoria não encontrada.");
}
