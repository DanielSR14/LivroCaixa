using LivroCaixa.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LivroCaixa.Infrastructure.Database.Configurations;

public class LancamentoConfiguration : IEntityTypeConfiguration<Lancamento>
{
    public void Configure(EntityTypeBuilder<Lancamento> builder)
    {
        builder.ToTable("Lancamentos");
        builder.HasKey(l => l.Id);

        builder.Property(l => l.Tipo)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(l => l.Data)
            .IsRequired();

        builder.Property(l => l.Valor)
            .IsRequired();

        builder.Property(l => l.FormaPagamento)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(l => l.Descricao)
            .HasMaxLength(300);

        builder.Property(l => l.Observacoes)
            .HasMaxLength(1000);

        builder.Property(l => l.CriadoEm)
            .IsRequired();

        builder.HasOne<Empresa>()
            .WithMany()
            .HasForeignKey(l => l.EmpresaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Conta>()
            .WithMany()
            .HasForeignKey(l => l.ContaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Categoria>()
            .WithMany()
            .HasForeignKey(l => l.CategoriaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Metadata
            .FindNavigation(nameof(Lancamento.Anexos))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}
