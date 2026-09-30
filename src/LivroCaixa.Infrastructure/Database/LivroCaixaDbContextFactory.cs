using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace LivroCaixa.Infrastructure.Database;

/// <summary>
/// Usada apenas pela ferramenta de linha de comando "dotnet ef" para gerar migrations
/// sem depender do processo de inicialização real do aplicativo.
/// </summary>
public class LivroCaixaDbContextFactory : IDesignTimeDbContextFactory<LivroCaixaDbContext>
{
    public LivroCaixaDbContext CreateDbContext(string[] args)
    {
        CaminhosAplicacao.GarantirDiretorios();

        var optionsBuilder = new DbContextOptionsBuilder<LivroCaixaDbContext>();
        optionsBuilder.UseSqlite($"Data Source={CaminhosAplicacao.CaminhoBancoDados}");

        return new LivroCaixaDbContext(optionsBuilder.Options);
    }
}
