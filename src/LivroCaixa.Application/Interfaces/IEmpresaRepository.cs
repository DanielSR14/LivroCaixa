using LivroCaixa.Domain.Entities;

namespace LivroCaixa.Application.Interfaces;

public interface IEmpresaRepository
{
    Task<Empresa?> ObterUnicaAsync();

    Task<bool> ExisteAlgumaAsync();

    Task AdicionarAsync(Empresa empresa);
}
