using LivroCaixa.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LivroCaixa.Infrastructure.Database.Configurations;

public class EmpresaConfiguration : IEntityTypeConfiguration<Empresa>
{
    public void Configure(EntityTypeBuilder<Empresa> builder)
    {
        builder.ToTable("Empresas");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.RazaoSocial)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.Cnpj)
            .IsRequired()
            .HasMaxLength(18);

        builder.Property(e => e.NomeFantasia)
            .HasMaxLength(200);

        builder.Property(e => e.Segmento)
            .HasMaxLength(100);

        builder.Property(e => e.DataCadastro)
            .IsRequired();
    }
}
