using LivroCaixa.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace LivroCaixa.Application;

public static class DependencyInjection
{
    public static IServiceCollection AdicionarApplication(this IServiceCollection services)
    {
        services.AddSingleton<SessaoAtual>();
        services.AddSingleton<ControleTentativasLogin>();
        services.AddScoped<CadastroInicialService>();
        services.AddScoped<AutenticacaoService>();
        services.AddScoped<ContaService>();
        services.AddScoped<CategoriaService>();
        services.AddScoped<LancamentoService>();
        services.AddScoped<AnexoService>();
        services.AddScoped<ExportacaoService>();
        services.AddScoped<ArmazenamentoService>();
        services.AddScoped<RelatorioService>();
        services.AddScoped<BackupService>();
        services.AddScoped<PerfilService>();
        services.AddScoped<PendenciasService>();
        services.AddScoped<SegurancaService>();
        services.AddScoped<ModeloLancamentoService>();
        services.AddScoped<TransferenciaService>();

        return services;
    }
}
