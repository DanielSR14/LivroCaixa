using LivroCaixa.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LivroCaixa.Infrastructure.Database.Configurations;

public class ContaConfiguration : IEntityTypeConfiguration<Conta>
{
    public void Configure(EntityTypeBuilder<Conta> builder)
    {
        builder.ToTable("Contas");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Nome)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(c => c.Tipo)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(c => c.SaldoInicial)
            .IsRequired();

        builder.Property(c => c.Agencia)
            .HasMaxLength(20);

        builder.Property(c => c.NumeroConta)
            .HasMaxLength(30);

        builder.Property(c => c.TipoContaBancaria)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.HasOne<Empresa>()
            .WithMany()
            .HasForeignKey(c => c.EmpresaId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
