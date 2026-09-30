using LivroCaixa.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LivroCaixa.Infrastructure.Database.Configurations;

public class UsuarioConfiguration : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> builder)
    {
        builder.ToTable("Usuarios");
        builder.HasKey(u => u.Id);

        builder.Property(u => u.Nome)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(u => u.SenhaHash)
            .IsRequired();

        builder.Property(u => u.Perfil)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(u => u.CriadoEm)
            .IsRequired();

        builder.HasOne<Empresa>()
            .WithMany()
            .HasForeignKey(u => u.EmpresaId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
