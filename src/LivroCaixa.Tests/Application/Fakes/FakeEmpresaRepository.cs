using LivroCaixa.Application.Interfaces;
using LivroCaixa.Domain.Entities;

namespace LivroCaixa.Tests.Application.Fakes;

public class FakeEmpresaRepository : IEmpresaRepository
{
    public List<Empresa> Empresas { get; } = new();

    public Task<Empresa?> ObterUnicaAsync() => Task.FromResult(Empresas.SingleOrDefault());

    public Task<bool> ExisteAlgumaAsync() => Task.FromResult(Empresas.Count > 0);

    public Task AdicionarAsync(Empresa empresa)
    {
        Empresas.Add(empresa);
        return Task.CompletedTask;
    }
}
