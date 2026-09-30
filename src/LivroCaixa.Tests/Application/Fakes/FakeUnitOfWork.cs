using LivroCaixa.Application.Interfaces;

namespace LivroCaixa.Tests.Application.Fakes;

public class FakeUnitOfWork : IUnitOfWork
{
    public int VezesSalvo { get; private set; }

    public Task SalvarAlteracoesAsync()
    {
        VezesSalvo++;
        return Task.CompletedTask;
    }

    public void DescartarAlteracoesPendentes()
    {
    }
}
