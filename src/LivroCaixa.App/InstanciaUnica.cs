using System.Diagnostics;
using System.Runtime.InteropServices;

namespace LivroCaixa.App;

/// <summary>
/// Usado quando o app já está aberto (ou abrindo) e o usuário clica no atalho de novo:
/// em vez de uma segunda cópia, a janela que já existe volta para a frente.
/// </summary>
internal static class InstanciaUnica
{
    private const int SwRestore = 9;

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern bool IsIconic(IntPtr hWnd);

    public static void AtivarJanelaDaInstanciaExistente()
    {
        try
        {
            using var atual = Process.GetCurrentProcess();
            foreach (var processo in Process.GetProcessesByName(atual.ProcessName))
            {
                using (processo)
                {
                    // Sem janela ainda (a outra cópia está no meio da abertura): não há o que ativar.
                    if (processo.Id == atual.Id || processo.MainWindowHandle == IntPtr.Zero)
                        continue;

                    if (IsIconic(processo.MainWindowHandle))
                        ShowWindow(processo.MainWindowHandle, SwRestore);
                    SetForegroundWindow(processo.MainWindowHandle);
                    return;
                }
            }
        }
        catch
        {
            // Melhor esforço: se não der para ativar a outra janela, só não abre uma segunda cópia.
        }
    }
}
