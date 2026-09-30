using LivroCaixa.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LivroCaixa.Infrastructure.Database.Configurations;

public class AnexoConfiguration : IEntityTypeConfiguration<Anexo>
{
    public void Configure(EntityTypeBuilder<Anexo> builder)
    {
        builder.ToTable("Anexos");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.CaminhoArquivo)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(a => a.NomeOriginal)
            .IsRequired()
            .HasMaxLength(260);

        builder.Property(a => a.TipoArquivo)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(10);

        builder.Property(a => a.TamanhoBytes)
            .IsRequired();

        builder.Property(a => a.CriadoEm)
            .IsRequired();

        builder.HasOne<Lancamento>()
            .WithMany(l => l.Anexos)
            .HasForeignKey(a => a.LancamentoId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
