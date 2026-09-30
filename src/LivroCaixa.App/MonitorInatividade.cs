using System.Runtime.InteropServices;
using System.Windows.Threading;

namespace LivroCaixa.App;

/// <summary>
/// Dispara <c>aoExpirar</c> quando o computador fica sem uso (teclado/mouse) pelo tempo configurado.
/// Usa o tempo ocioso do Windows inteiro (GetLastInputInfo), não só deste app: quem está
/// trabalhando em outro programa (ex: conferindo uma planilha) não é bloqueado à toa — o
/// objetivo é proteger os dados quando a pessoa sai de perto do computador.
/// </summary>
public sealed class MonitorInatividade
{
    private static readonly TimeSpan IntervaloVerificacao = TimeSpan.FromSeconds(15);

    private readonly DispatcherTimer _timer;
    private readonly Action _aoExpirar;
    private bool _executandoAcao;

    /// <summary>0 = nunca bloquear.</summary>
    public int MinutosLimite { get; set; }

    public MonitorInatividade(Action aoExpirar)
    {
        _aoExpirar = aoExpirar;
        _timer = new DispatcherTimer { Interval = IntervaloVerificacao };
        _timer.Tick += (_, _) => Verificar();
    }

    public void Iniciar() => _timer.Start();

    public void Parar() => _timer.Stop();

    private void Verificar()
    {
        // _aoExpirar abre uma janela modal, e o DispatcherTimer continua disparando dentro do
        // laço de mensagens dela — sem esta trava, a tela de bloqueio seria aberta de novo a cada 15s.
        if (_executandoAcao || MinutosLimite <= 0 || ObterTempoOcioso() < TimeSpan.FromMinutes(MinutosLimite))
            return;

        _executandoAcao = true;
        try
        {
            _aoExpirar();
        }
        finally
        {
            _executandoAcao = false;
        }
    }

    private static TimeSpan ObterTempoOcioso()
    {
        var info = new LastInputInfo { TamanhoEstrutura = (uint)Marshal.SizeOf<LastInputInfo>() };
        if (!GetLastInputInfo(ref info))
            return TimeSpan.Zero;

        // Ambos em milissegundos desde o boot (32 bits); a subtração sem sinal trata a virada do contador.
        return TimeSpan.FromMilliseconds(unchecked((uint)Environment.TickCount - info.MomentoUltimaEntrada));
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct LastInputInfo
    {
        public uint TamanhoEstrutura;
        public uint MomentoUltimaEntrada;
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetLastInputInfo(ref LastInputInfo informacao);
}
