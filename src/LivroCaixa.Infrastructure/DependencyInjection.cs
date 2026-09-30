using LivroCaixa.Application.Interfaces;
using LivroCaixa.Infrastructure.Database;
using LivroCaixa.Infrastructure.Export;
using LivroCaixa.Infrastructure.Files;
using LivroCaixa.Infrastructure.Logs;
using LivroCaixa.Infrastructure.Repositories;
using LivroCaixa.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LivroCaixa.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AdicionarInfraestrutura(this IServiceCollection services)
    {
        CaminhosAplicacao.GarantirDiretorios();

        // Atenção ao tempo de vida real: as telas principais (MainWindow → ShellViewModel →
        // serviços) são resolvidas direto do provedor raiz, sem escopo, então na prática um único
        // DbContext atende a janela principal inteira enquanto o app está aberto. Por isso as
        // listagens usam AsNoTracking e trabalho em segundo plano cria o próprio escopo
        // (ver ShellViewModel.ExecutarBackupAutomaticoEmSegundoPlanoAsync).
        services.AddDbContext<LivroCaixaDbContext>(options =>
            options.UseSqlite($"Data Source={CaminhosAplicacao.CaminhoBancoDados}"));

        services.AddScoped<IUnitOfWork>(provedor => provedor.GetRequiredService<LivroCaixaDbContext>());
        services.AddScoped<IEmpresaRepository, EmpresaRepository>();
        services.AddScoped<IUsuarioRepository, UsuarioRepository>();
        services.AddScoped<IContaRepository, ContaRepository>();
        services.AddScoped<ICategoriaRepository, CategoriaRepository>();
        services.AddScoped<ILancamentoRepository, LancamentoRepository>();
        services.AddScoped<IRemessaExportacaoRepository, RemessaExportacaoRepository>();
        services.AddScoped<IConfiguracaoBackupRepository, ConfiguracaoBackupRepository>();
        services.AddScoped<IModeloLancamentoRepository, ModeloLancamentoRepository>();
        services.AddScoped<ITransferenciaRepository, TransferenciaRepository>();
        services.AddScoped<IPasswordHasher, BCryptPasswordHasher>();
        services.AddScoped<IArmazenamentoAnexoService, ArmazenamentoAnexoService>();
        services.AddScoped<IExportadorArquivoService, ExportadorArquivoService>();
        services.AddScoped<IBackupArquivoService, BackupArquivoService>();
        services.AddSingleton<IRegistroLog, RegistroLogArquivo>();

        return services;
    }

    /// <summary>
    /// Aplica as migrations pendentes no banco local. Deve ser chamado uma vez na
    /// inicialização do app, para que uma atualização já traga o schema em dia
    /// sem exigir nenhuma ação manual do cliente.
    /// </summary>
    public static void AplicarMigracoesPendentes(this IServiceProvider serviceProvider)
    {
        using var escopo = serviceProvider.CreateScope();
        var dbContext = escopo.ServiceProvider.GetRequiredService<LivroCaixaDbContext>();
        dbContext.Database.Migrate();
    }
}
