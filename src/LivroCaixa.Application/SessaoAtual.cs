namespace LivroCaixa.Application;

/// <summary>
/// Estado da sessão do usuário autenticado no aplicativo (instância única por execução).
/// Definido após o cadastro inicial ou o login, e consultado pelas telas seguintes.
/// </summary>
public class SessaoAtual
{
    public Guid EmpresaId { get; private set; }
    public string NomeEmpresa { get; private set; } = string.Empty;
    public Guid UsuarioId { get; private set; }
    public string NomeUsuario { get; private set; } = string.Empty;

    public bool EstaAutenticado => EmpresaId != Guid.Empty && UsuarioId != Guid.Empty;

    public void Definir(Guid empresaId, string nomeEmpresa, Guid usuarioId, string nomeUsuario)
    {
        EmpresaId = empresaId;
        NomeEmpresa = nomeEmpresa;
        UsuarioId = usuarioId;
        NomeUsuario = nomeUsuario;
    }
}
