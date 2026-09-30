using LivroCaixa.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LivroCaixa.Infrastructure.Database.Configurations;

public class RemessaExportacaoConfiguration : IEntityTypeConfiguration<RemessaExportacao>
{
    public void Configure(EntityTypeBuilder<RemessaExportacao> builder)
    {
        builder.ToTable("RemessasExportacao");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Ano).IsRequired();
        builder.Property(r => r.Mes).IsRequired();
        builder.Property(r => r.GeradoEm).IsRequired();
        builder.Property(r => r.QuantidadeLancamentos).IsRequired();

        builder.Property(r => r.CaminhoArquivoGerado)
            .IsRequired()
            .HasMaxLength(500);

        builder.HasOne<Empresa>()
            .WithMany()
            .HasForeignKey(r => r.EmpresaId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(r => new { r.EmpresaId, r.Ano, r.Mes });
    }
}
