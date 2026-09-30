using LivroCaixa.Application.Interfaces;
using LivroCaixa.Domain.Entities;
using LivroCaixa.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace LivroCaixa.Infrastructure.Repositories;

public class UsuarioRepository : IUsuarioRepository
{
    private readonly LivroCaixaDbContext _dbContext;

    public UsuarioRepository(LivroCaixaDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    // Comparação em memória, não no SQL: o "=" do SQLite diferencia maiúsculas e o LOWER() dele só
    // conhece ASCII ("JOÃO" vira "joÃo"). São poucos usuários por empresa, então carregar todos é barato.
    public async Task<Usuario?> ObterPorNomeAsync(Guid empresaId, string nome)
    {
        var usuarios = await _dbContext.Usuarios.Where(u => u.EmpresaId == empresaId).ToListAsync();
        return usuarios.SingleOrDefault(u => string.Equals(u.Nome, nome.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    public Task<Usuario?> ObterPorIdAsync(Guid id) =>
        _dbContext.Usuarios.SingleOrDefaultAsync(u => u.Id == id);

    public async Task AdicionarAsync(Usuario usuario) => await _dbContext.Usuarios.AddAsync(usuario);
}
