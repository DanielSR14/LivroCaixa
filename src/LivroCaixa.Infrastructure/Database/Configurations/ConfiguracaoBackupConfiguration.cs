using LivroCaixa.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LivroCaixa.Infrastructure.Database.Configurations;

public class ConfiguracaoBackupConfiguration : IEntityTypeConfiguration<ConfiguracaoBackup>
{
    public void Configure(EntityTypeBuilder<ConfiguracaoBackup> builder)
    {
        builder.ToTable("ConfiguracoesBackup");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Ativo)
            .IsRequired();

        builder.Property(c => c.Frequencia)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(c => c.PastaDestino)
            .HasMaxLength(500);

        builder.HasOne<Empresa>()
            .WithMany()
            .HasForeignKey(c => c.EmpresaId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(c => c.EmpresaId)
            .IsUnique();
    }
}
