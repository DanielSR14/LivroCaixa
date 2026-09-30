using LivroCaixa.Domain.Enums;
using LivroCaixa.Domain.Exceptions;

namespace LivroCaixa.Domain.Entities;

/// <summary>
/// Lançamento recorrente pré-configurado (ex: "ALU" = aluguel da sede). Digitar o código no
/// formulário de lançamento preenche categoria, conta, forma de pagamento, descrição e valor
/// padrão — o usuário sempre revisa e confirma antes de salvar. Nunca gera lançamento sozinho
/// (ver IDEIAS_E_MELHORIAS.md, item 10, sobre o risco da geração automática).
/// </summary>
public class ModeloLancamento : EntidadeBase
{
    public const int TamanhoMaximoCodigo = 10;

    public Guid EmpresaId { get; private set; }
    public string Codigo { get; private set; } = null!;
    public string Nome { get; private set; } = null!;
    public TipoLancamento Tipo { get; private set; }
    public Guid CategoriaId { get; private set; }

    /// <summary>Opcional: sem conta, o formulário mantém a conta que já estava selecionada.</summary>
    public Guid? ContaId { get; private set; }

    public FormaPagamento FormaPagamento { get; private set; }

    /// <summary>Opcional: valores que variam todo mês (ex: conta de luz) ficam sem valor padrão.</summary>
    public decimal? Valor { get; private set; }

    public string? Descricao { get; private set; }

    private ModeloLancamento()
    {
    }

    public ModeloLancamento(
        Guid empresaId,
        string codigo,
        string nome,
        Categoria categoria,
        Conta? conta,
        FormaPagamento formaPagamento,
        decimal? valor,
        string? descricao)
    {
        if (empresaId == Guid.Empty)
            throw new DomainException("O modelo precisa estar vinculado a uma empresa.");

        EmpresaId = empresaId;
        Definir(codigo, nome, categoria, conta, formaPagamento, valor, descricao);
    }

    public void Atualizar(
        string codigo,
        string nome,
        Categoria categoria,
        Conta? conta,
        FormaPagamento formaPagamento,
        decimal? valor,
        string? descricao) =>
        Definir(codigo, nome, categoria, conta, formaPagamento, valor, descricao);

    /// <summary>Forma canônica do código (maiúsculas, sem espaços nas pontas) — usada também na busca.</summary>
    public static string NormalizarCodigo(string? codigo) => (codigo ?? string.Empty).Trim().ToUpperInvariant();

    private void Definir(
        string codigo,
        string nome,
        Categoria categoria,
        Conta? conta,
        FormaPagamento formaPagamento,
        decimal? valor,
        string? descricao)
    {
        var codigoNormalizado = NormalizarCodigo(codigo);
        if (codigoNormalizado.Length == 0)
            throw new DomainException("O código do modelo é obrigatório.");
        if (codigoNormalizado.Length > TamanhoMaximoCodigo)
            throw new DomainException($"O código do modelo pode ter no máximo {TamanhoMaximoCodigo} caracteres.");
        if (codigoNormalizado.Any(char.IsWhiteSpace))
            throw new DomainException("O código do modelo não pode ter espaços.");

        if (string.IsNullOrWhiteSpace(nome))
            throw new DomainException("O nome do modelo é obrigatório.");

        if (categoria is null)
            throw new DomainException("Selecione uma categoria para o modelo.");
        if (categoria.EmpresaId != EmpresaId)
            throw new DomainException("A categoria selecionada não pertence a esta empresa.");
        if (conta is not null && conta.EmpresaId != EmpresaId)
            throw new DomainException("A conta selecionada não pertence a esta empresa.");

        if (valor is <= 0)
            throw new DomainException("O valor padrão do modelo deve ser maior que zero (ou ficar em branco).");

        Codigo = codigoNormalizado;
        Nome = nome.Trim();
        // O tipo vem da categoria: um modelo nunca fica com entrada/saída incoerente com ela.
        Tipo = categoria.Tipo;
        CategoriaId = categoria.Id;
        ContaId = conta?.Id;
        FormaPagamento = formaPagamento;
        Valor = valor;
        Descricao = string.IsNullOrWhiteSpace(descricao) ? null : descricao.Trim();
    }
}
