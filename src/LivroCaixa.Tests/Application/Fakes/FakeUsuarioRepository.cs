using LivroCaixa.Application.Interfaces;
using LivroCaixa.Domain.Entities;

namespace LivroCaixa.Tests.Application.Fakes;

public class FakeUsuarioRepository : IUsuarioRepository
{
    public List<Usuario> Usuarios { get; } = new();

    public Task<Usuario?> ObterPorNomeAsync(Guid empresaId, string nome) =>
        Task.FromResult(Usuarios.SingleOrDefault(u =>
            u.EmpresaId == empresaId && string.Equals(u.Nome, nome.Trim(), StringComparison.OrdinalIgnoreCase)));

    public Task<Usuario?> ObterPorIdAsync(Guid id) =>
        Task.FromResult(Usuarios.SingleOrDefault(u => u.Id == id));

    public Task AdicionarAsync(Usuario usuario)
    {
        Usuarios.Add(usuario);
        return Task.CompletedTask;
    }
}
