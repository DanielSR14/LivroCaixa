using LivroCaixa.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LivroCaixa.Infrastructure.Database.Configurations;

public class HistoricoLancamentoConfiguration : IEntityTypeConfiguration<HistoricoLancamento>
{
    public void Configure(EntityTypeBuilder<HistoricoLancamento> builder)
    {
        builder.ToTable("HistoricosLancamento");
        builder.HasKey(h => h.Id);

        builder.Property(h => h.TipoEvento)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(h => h.DataHora)
            .IsRequired();

        builder.Property(h => h.DadosAnteriores)
            .HasMaxLength(2000);

        builder.HasOne<Empresa>()
            .WithMany()
            .HasForeignKey(h => h.EmpresaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Lancamento>()
            .WithMany()
            .HasForeignKey(h => h.LancamentoId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(h => h.LancamentoId);
        builder.HasIndex(h => new { h.EmpresaId, h.DataHora });
    }
}
