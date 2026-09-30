using LivroCaixa.Domain.Exceptions;

namespace LivroCaixa.Application;

/// <summary>
/// Bloqueio temporário por tentativas repetidas de login erradas (mitigação de força bruta local —
/// ver SEGURANCA.md, seção 6). Estado em memória, por nome de usuário tentado, perdido a cada
/// reinício do app — não precisa persistir em banco, é só um atraso, não uma auditoria.
/// </summary>
public class ControleTentativasLogin
{
    private const int LimiteTentativas = 5;
    private static readonly TimeSpan TempoBloqueio = TimeSpan.FromMinutes(1);

    private readonly Dictionary<string, (int Tentativas, DateTime? BloqueadoAte)> _estado =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly object _travaEstado = new();

    /// <summary>Quanto tempo falta pro bloqueio acabar, ou null se não está bloqueado.</summary>
    public TimeSpan? ObterTempoRestanteDeBloqueio(string nomeUsuario)
    {
        lock (_travaEstado)
        {
            if (!_estado.TryGetValue(Normalizar(nomeUsuario), out var info) || info.BloqueadoAte is null)
                return null;

            var restante = info.BloqueadoAte.Value - DateTime.UtcNow;
            return restante > TimeSpan.Zero ? restante : null;
        }
    }

    /// <summary>Lança DomainException com o tempo de espera, se a chave estiver bloqueada por tentativas erradas.</summary>
    public void GarantirQueNaoEstaBloqueado(string nomeUsuario)
    {
        var tempoRestante = ObterTempoRestanteDeBloqueio(nomeUsuario);
        if (tempoRestante is null)
            return;

        var segundos = (int)Math.Ceiling(tempoRestante.Value.TotalSeconds);
        throw new DomainException($"Muitas tentativas erradas. Tente de novo em {segundos} segundo{(segundos == 1 ? "" : "s")}.");
    }

    public void RegistrarFalha(string nomeUsuario)
    {
        lock (_travaEstado)
        {
            var chave = Normalizar(nomeUsuario);
            var tentativas = (_estado.TryGetValue(chave, out var info) ? info.Tentativas : 0) + 1;
            DateTime? bloqueadoAte = tentativas >= LimiteTentativas ? DateTime.UtcNow.Add(TempoBloqueio) : null;
            _estado[chave] = (tentativas, bloqueadoAte);
        }
    }

    public void RegistrarSucesso(string nomeUsuario)
    {
        lock (_travaEstado)
        {
            _estado.Remove(Normalizar(nomeUsuario));
        }
    }

    private static string Normalizar(string nomeUsuario) => nomeUsuario.Trim();
}
