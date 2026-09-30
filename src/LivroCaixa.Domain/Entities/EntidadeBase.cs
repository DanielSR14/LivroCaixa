namespace LivroCaixa.Domain.Entities;

public abstract class EntidadeBase
{
    public Guid Id { get; protected set; }

    protected EntidadeBase()
    {
        Id = Guid.NewGuid();
    }
}
