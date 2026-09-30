using LivroCaixa.Application.Interfaces;
using LivroCaixa.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LivroCaixa.Infrastructure.Database;

public class LivroCaixaDbContext : DbContext, IUnitOfWork
{
    public DbSet<Empresa> Empresas => Set<Empresa>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Conta> Contas => Set<Conta>();
    public DbSet<Categoria> Categorias => Set<Categoria>();
    public DbSet<Lancamento> Lancamentos => Set<Lancamento>();
    public DbSet<Anexo> Anexos => Set<Anexo>();
    public DbSet<RemessaExportacao> RemessasExportacao => Set<RemessaExportacao>();
    public DbSet<HistoricoLancamento> HistoricosLancamento => Set<HistoricoLancamento>();
    public DbSet<ConfiguracaoBackup> ConfiguracoesBackup => Set<ConfiguracaoBackup>();
    public DbSet<ModeloLancamento> ModelosLancamento => Set<ModeloLancamento>();
    public DbSet<Transferencia> Transferencias => Set<Transferencia>();

    public LivroCaixaDbContext(DbContextOptions<LivroCaixaDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LivroCaixaDbContext).Assembly);
    }

    public async Task SalvarAlteracoesAsync() => await SaveChangesAsync();

    public void DescartarAlteracoesPendentes() => ChangeTracker.Clear();
}
