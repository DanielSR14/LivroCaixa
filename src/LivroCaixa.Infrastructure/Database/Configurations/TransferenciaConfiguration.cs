using LivroCaixa.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LivroCaixa.Infrastructure.Database.Configurations;

public class TransferenciaConfiguration : IEntityTypeConfiguration<Transferencia>
{
    public void Configure(EntityTypeBuilder<Transferencia> builder)
    {
        builder.ToTable("Transferencias");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Data).IsRequired();
        builder.Property(t => t.Valor).IsRequired();
        builder.Property(t => t.Descricao).HasMaxLength(300);
        builder.Property(t => t.CriadoEm).IsRequired();

        builder.HasOne<Empresa>()
            .WithMany()
            .HasForeignKey(t => t.EmpresaId)
            .OnDelete(DeleteBehavior.Restrict);

        // Restrict, como em Lancamento: a conta não pode sumir levando junto o histórico de movimentação.
        builder.HasOne<Conta>()
            .WithMany()
            .HasForeignKey(t => t.ContaOrigemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Conta>()
            .WithMany()
            .HasForeignKey(t => t.ContaDestinoId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
