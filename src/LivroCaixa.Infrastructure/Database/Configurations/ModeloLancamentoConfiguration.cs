using LivroCaixa.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LivroCaixa.Infrastructure.Database.Configurations;

public class ModeloLancamentoConfiguration : IEntityTypeConfiguration<ModeloLancamento>
{
    public void Configure(EntityTypeBuilder<ModeloLancamento> builder)
    {
        builder.ToTable("ModelosLancamento");
        builder.HasKey(m => m.Id);

        builder.Property(m => m.Codigo)
            .IsRequired()
            .HasMaxLength(ModeloLancamento.TamanhoMaximoCodigo);

        builder.HasIndex(m => new { m.EmpresaId, m.Codigo })
            .IsUnique();

        builder.Property(m => m.Nome)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(m => m.Tipo)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(m => m.FormaPagamento)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(m => m.Descricao)
            .HasMaxLength(300);

        builder.HasOne<Empresa>()
            .WithMany()
            .HasForeignKey(m => m.EmpresaId)
            .OnDelete(DeleteBehavior.Cascade);

        // Diferente de Lancamento (Restrict), aqui a exclusão da categoria/conta não é bloqueada:
        // um modelo é só um atalho, não um registro contábil. Sem a categoria ele perde o sentido
        // (é apagado junto); sem a conta ele continua útil, só deixa de sugerir uma conta.
        builder.HasOne<Categoria>()
            .WithMany()
            .HasForeignKey(m => m.CategoriaId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Conta>()
            .WithMany()
            .HasForeignKey(m => m.ContaId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
