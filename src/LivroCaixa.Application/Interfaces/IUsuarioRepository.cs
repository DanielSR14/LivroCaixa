using LivroCaixa.Domain.Entities;

namespace LivroCaixa.Application.Interfaces;

public interface IUsuarioRepository
{
    /// <summary>Ignora maiúsculas/minúsculas (inclusive acentuadas) e espaços nas pontas do nome digitado.</summary>
    Task<Usuario?> ObterPorNomeAsync(Guid empresaId, string nome);

    Task<Usuario?> ObterPorIdAsync(Guid id);

    Task AdicionarAsync(Usuario usuario);
}
