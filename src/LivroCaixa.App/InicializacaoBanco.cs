using LivroCaixa.Application.Services;
using LivroCaixa.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace LivroCaixa.App;

/// <summary>
/// Migrations + primeira consulta do EF Core (montagem do modelo, compilação) custam mais de um
/// segundo na abertura. Rodam em segundo plano enquanto a tela de login/cadastro já aparece; toda
/// ação dessas telas que toca o banco aguarda <see cref="AguardarAsync"/> antes (na prática já
/// terminou quando o usuário acaba de digitar a senha). Uma falha aqui aparece na primeira ação
/// que aguardar, e é tratada como erro fatal de abertura (ver App.AoOcorrerErroNaoTratado).
/// </summary>
public class InicializacaoBanco
{
    private Task _tarefa = Task.CompletedTask;

    public void Iniciar(IServiceProvider servicos) => _tarefa = Task.Run(async () =>
    {
        // Escopo próprio: o DbContext da janela principal (provedor raiz) não pode ser usado
        // fora da thread da UI. O modelo e as consultas compiladas ficam em cache compartilhado.
        servicos.AplicarMigracoesPendentes();
        using var escopo = servicos.CreateScope();
        await escopo.ServiceProvider.GetRequiredService<AutenticacaoService>().ExisteEmpresaCadastradaAsync();
    });

    public Task AguardarAsync() => _tarefa;
}
