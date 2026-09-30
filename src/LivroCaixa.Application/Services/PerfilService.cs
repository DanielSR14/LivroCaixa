using LivroCaixa.Application.DTOs;
using LivroCaixa.Application.Interfaces;
using LivroCaixa.Domain.Exceptions;

namespace LivroCaixa.Application.Services;

/// <summary>Dados de empresa e usuário logado para exibição (ex: menu do avatar na sidebar).</summary>
public class PerfilService
{
    private readonly IEmpresaRepository _empresaRepository;
    private readonly IUsuarioRepository _usuarioRepository;

    public PerfilService(IEmpresaRepository empresaRepository, IUsuarioRepository usuarioRepository)
    {
        _empresaRepository = empresaRepository;
        _usuarioRepository = usuarioRepository;
    }

    public async Task<PerfilDto> ObterAsync(Guid usuarioId)
    {
        var usuario = await _usuarioRepository.ObterPorIdAsync(usuarioId)
            ?? throw new DomainException("Usuário não encontrado.");
        var empresa = await _empresaRepository.ObterUnicaAsync()
            ?? throw new DomainException("Empresa não encontrada.");

        return new PerfilDto(
            usuario.Nome, usuario.Perfil, empresa.NomeExibicao, empresa.RazaoSocial,
            empresa.Cnpj, empresa.Segmento, empresa.DataCadastro);
    }
}
