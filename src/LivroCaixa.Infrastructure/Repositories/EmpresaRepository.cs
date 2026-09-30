using LivroCaixa.Application.Interfaces;
using LivroCaixa.Domain.Entities;
using LivroCaixa.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace LivroCaixa.Infrastructure.Repositories;

public class EmpresaRepository : IEmpresaRepository
{
    private readonly LivroCaixaDbContext _dbContext;

    public EmpresaRepository(LivroCaixaDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Empresa?> ObterUnicaAsync() => _dbContext.Empresas.SingleOrDefaultAsync();

    public Task<bool> ExisteAlgumaAsync() => _dbContext.Empresas.AnyAsync();

    public async Task AdicionarAsync(Empresa empresa) => await _dbContext.Empresas.AddAsync(empresa);
}
