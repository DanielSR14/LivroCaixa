using LivroCaixa.Application.Interfaces;
using LivroCaixa.Domain.Entities;
using LivroCaixa.Domain.Exceptions;

namespace LivroCaixa.Application.Services;

/// <summary>
/// Proteção de acesso além do login em si: bloqueio por inatividade, código de recuperação
/// para senha esquecida e aceite do termo de uso/privacidade.
/// </summary>
public class SegurancaService
{
    // Chave separada da do login no ControleTentativasLogin: errar o código de recuperação não
    // deve bloquear o login com a senha (e vice-versa).
    private const string PrefixoTentativasRecuperacao = "recuperacao|";

    private readonly IEmpresaRepository _empresaRepository;
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ControleTentativasLogin _controleTentativas;
    private readonly IUnitOfWork _unitOfWork;

    public SegurancaService(
        IEmpresaRepository empresaRepository,
        IUsuarioRepository usuarioRepository,
        IPasswordHasher passwordHasher,
        ControleTentativasLogin controleTentativas,
        IUnitOfWork unitOfWork)
    {
        _empresaRepository = empresaRepository;
        _usuarioRepository = usuarioRepository;
        _passwordHasher = passwordHasher;
        _controleTentativas = controleTentativas;
        _unitOfWork = unitOfWork;
    }

    public async Task<int> ObterMinutosBloqueioInatividadeAsync() =>
        (await ObterEmpresaOuFalharAsync()).MinutosBloqueioInatividade;

    public async Task DefinirMinutosBloqueioInatividadeAsync(int minutos)
    {
        var empresa = await ObterEmpresaOuFalharAsync();
        empresa.DefinirBloqueioInatividade(minutos);
        await _unitOfWork.SalvarAlteracoesAsync();
    }

    /// <summary>Senha digitada na tela de bloqueio. Conta como tentativa de login para o bloqueio por erros repetidos.</summary>
    public async Task<bool> ConfirmarSenhaAsync(Guid usuarioId, string senha)
    {
        var usuario = await ObterUsuarioOuFalharAsync(usuarioId);
        _controleTentativas.GarantirQueNaoEstaBloqueado(usuario.Nome);

        if (!_passwordHasher.VerificarSenha(senha, usuario.SenhaHash))
        {
            _controleTentativas.RegistrarFalha(usuario.Nome);
            return false;
        }

        _controleTentativas.RegistrarSucesso(usuario.Nome);
        return true;
    }

    public async Task<bool> PrecisaAceitarTermoAsync(string versaoAtual) =>
        !(await ObterEmpresaOuFalharAsync()).AceitouTermo(versaoAtual);

    public async Task RegistrarAceiteTermoAsync(string versao)
    {
        var empresa = await ObterEmpresaOuFalharAsync();
        empresa.AceitarTermo(versao);
        await _unitOfWork.SalvarAlteracoesAsync();
    }

    public async Task<bool> PrecisaGerarCodigoRecuperacaoAsync(Guid usuarioId) =>
        !(await ObterUsuarioOuFalharAsync(usuarioId)).TemCodigoRecuperacao;

    /// <summary>
    /// Passa a valer o código informado (gerado com <see cref="CodigoRecuperacao.Gerar"/>), invalidando
    /// o anterior. Separado da geração de propósito: a tela só grava depois de o usuário confirmar
    /// que guardou o código — se o app fechar antes disso, o código antigo continua valendo.
    /// </summary>
    public async Task DefinirCodigoRecuperacaoAsync(Guid usuarioId, string codigo)
    {
        var usuario = await ObterUsuarioOuFalharAsync(usuarioId);
        usuario.DefinirCodigoRecuperacao(_passwordHasher.GerarHash(CodigoRecuperacao.Normalizar(codigo)));
        await _unitOfWork.SalvarAlteracoesAsync();
    }

    /// <summary>
    /// Troca a senha usando o código de recuperação. O código usado deixa de valer e um novo é
    /// devolvido para o usuário guardar. Erro genérico de propósito: não revela se o usuário existe.
    /// </summary>
    public async Task<string> RecuperarAcessoAsync(string nomeUsuario, string codigoRecuperacao, string novaSenha)
    {
        var chaveTentativas = PrefixoTentativasRecuperacao + nomeUsuario;
        _controleTentativas.GarantirQueNaoEstaBloqueado(chaveTentativas);
        RegrasSenha.Validar(novaSenha);

        var empresa = await ObterEmpresaOuFalharAsync();
        var usuario = await _usuarioRepository.ObterPorNomeAsync(empresa.Id, nomeUsuario);
        var codigoNormalizado = CodigoRecuperacao.Normalizar(codigoRecuperacao);

        if (usuario?.CodigoRecuperacaoHash is null
            || !_passwordHasher.VerificarSenha(codigoNormalizado, usuario.CodigoRecuperacaoHash))
        {
            _controleTentativas.RegistrarFalha(chaveTentativas);
            throw new DomainException("Usuário ou código de recuperação inválidos.");
        }

        _controleTentativas.RegistrarSucesso(chaveTentativas);
        _controleTentativas.RegistrarSucesso(usuario.Nome);
        // Aqui o código novo é gravado junto com a senha (mesma transação): o antigo acabou de ser
        // usado e não pode continuar valendo. Se o app fechar antes de a pessoa ver o novo, ela já
        // tem a senha nova e pode gerar outro código em Configurações.
        usuario.RedefinirSenha(_passwordHasher.GerarHash(novaSenha));
        var novoCodigo = CodigoRecuperacao.Gerar();
        usuario.DefinirCodigoRecuperacao(_passwordHasher.GerarHash(CodigoRecuperacao.Normalizar(novoCodigo)));
        await _unitOfWork.SalvarAlteracoesAsync();

        return novoCodigo;
    }

    private async Task<Empresa> ObterEmpresaOuFalharAsync() =>
        await _empresaRepository.ObterUnicaAsync() ?? throw new DomainException("Empresa não encontrada.");

    private async Task<Usuario> ObterUsuarioOuFalharAsync(Guid usuarioId) =>
        await _usuarioRepository.ObterPorIdAsync(usuarioId) ?? throw new DomainException("Usuário não encontrado.");
}
