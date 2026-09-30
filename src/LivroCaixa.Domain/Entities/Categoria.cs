using LivroCaixa.Domain.Enums;
using LivroCaixa.Domain.Exceptions;

namespace LivroCaixa.Domain.Entities;

public class Categoria : EntidadeBase
{
    public Guid EmpresaId { get; private set; }
    public string Nome { get; private set; } = null!;
    public TipoLancamento Tipo { get; private set; }

    private Categoria()
    {
    }

    public Categoria(Guid empresaId, string nome, TipoLancamento tipo)
    {
        if (empresaId == Guid.Empty)
            throw new DomainException("A categoria precisa estar vinculada a uma empresa.");

        if (string.IsNullOrWhiteSpace(nome))
            throw new DomainException("O nome da categoria é obrigatório.");

        EmpresaId = empresaId;
        Nome = nome.Trim();
        Tipo = tipo;
    }

    /// <summary>Só o nome pode ser alterado — o Tipo (entrada/saída) é fixo desde a criação,
    /// mesma regra já aplicada a Lancamento.Tipo, para nunca deixar lançamentos existentes
    /// inconsistentes com a categoria a que pertencem.</summary>
    public void Atualizar(string nome)
    {
        if (string.IsNullOrWhiteSpace(nome))
            throw new DomainException("O nome da categoria é obrigatório.");

        Nome = nome.Trim();
    }
}
