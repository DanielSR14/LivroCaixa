using LivroCaixa.Application.DTOs;
using LivroCaixa.Application.Interfaces;
using LivroCaixa.Domain.Entities;
using LivroCaixa.Domain.Exceptions;

namespace LivroCaixa.Application.Services;

public class AutenticacaoService
{
    private readonly IEmpresaRepository _empresaRepository;
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ControleTentativasLogin _controleTentativas;

    public AutenticacaoService(
        IEmpresaRepository empresaRepository,
        IUsuarioRepository usuarioRepository,
        IPasswordHasher passwordHasher,
        ControleTentativasLogin controleTentativas)
    {
        _empresaRepository = empresaRepository;
        _usuarioRepository = usuarioRepository;
        _passwordHasher = passwordHasher;
        _controleTentativas = controleTentativas;
    }

    public Task<bool> ExisteEmpresaCadastradaAsync() => _empresaRepository.ExisteAlgumaAsync();

    public async Task<(Usuario Usuario, Empresa Empresa)?> AutenticarAsync(LoginDto dto)
    {
        _controleTentativas.GarantirQueNaoEstaBloqueado(dto.NomeUsuario);

        var empresa = await _empresaRepository.ObterUnicaAsync();
        if (empresa is null)
            return null;

        var usuario = await _usuarioRepository.ObterPorNomeAsync(empresa.Id, dto.NomeUsuario);
        if (usuario is null || !_passwordHasher.VerificarSenha(dto.Senha, usuario.SenhaHash))
        {
            _controleTentativas.RegistrarFalha(dto.NomeUsuario);
            return null;
        }

        _controleTentativas.RegistrarSucesso(dto.NomeUsuario);
        return (usuario, empresa);
    }
}
