using LivroCaixa.Application.DTOs;
using LivroCaixa.Application.Interfaces;
using LivroCaixa.Domain.Entities;
using LivroCaixa.Domain.Enums;
using LivroCaixa.Domain.Exceptions;

namespace LivroCaixa.Application.Services;

/// <summary>
/// Caso de uso executado uma única vez por instalação: cria a empresa, o usuário
/// administrador e os dados iniciais (conta "Caixa" e categorias padrão).
/// </summary>
public class CadastroInicialService
{
    private readonly IEmpresaRepository _empresaRepository;
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IContaRepository _contaRepository;
    private readonly ICategoriaRepository _categoriaRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IUnitOfWork _unitOfWork;

    public CadastroInicialService(
        IEmpresaRepository empresaRepository,
        IUsuarioRepository usuarioRepository,
        IContaRepository contaRepository,
        ICategoriaRepository categoriaRepository,
        IPasswordHasher passwordHasher,
        IUnitOfWork unitOfWork)
    {
        _empresaRepository = empresaRepository;
        _usuarioRepository = usuarioRepository;
        _contaRepository = contaRepository;
        _categoriaRepository = categoriaRepository;
        _passwordHasher = passwordHasher;
        _unitOfWork = unitOfWork;
    }

    public async Task<Usuario> CadastrarAsync(CadastroInicialDto dto)
    {
        if (await _empresaRepository.ExisteAlgumaAsync())
            throw new DomainException("Já existe uma empresa cadastrada neste computador.");

        RegrasSenha.Validar(dto.Senha);

        var empresa = new Empresa(dto.RazaoSocial, dto.Cnpj, dto.Segmento, dto.NomeFantasia);
        // O termo é aceito antes do cadastro (LGPD: antes de coletar os dados), mas só há onde
        // registrar o aceite depois que a empresa existe.
        if (dto.VersaoTermoAceita is not null)
            empresa.AceitarTermo(dto.VersaoTermoAceita);
        await _empresaRepository.AdicionarAsync(empresa);

        var senhaHash = _passwordHasher.GerarHash(dto.Senha);
        var usuario = new Usuario(empresa.Id, dto.NomeUsuario, senhaHash, PerfilUsuario.Administrador);
        await _usuarioRepository.AdicionarAsync(usuario);

        var contaCaixa = new Conta(empresa.Id, "Caixa", TipoConta.Caixa);
        await _contaRepository.AdicionarAsync(contaCaixa);

        var categorias = CategoriasPadrao.Criar(empresa.Id, dto.Segmento);
        await _categoriaRepository.AdicionarVariasAsync(categorias);

        await _unitOfWork.SalvarAlteracoesAsync();

        return usuario;
    }
}
