using LivroCaixa.Domain.Entities;
using LivroCaixa.Domain.Exceptions;

namespace LivroCaixa.Tests.Domain;

public class EmpresaTests
{
    [Fact]
    public void Deve_criar_empresa_com_dados_validos()
    {
        var empresa = new Empresa("Padaria Pão Quente Ltda", "12.345.678/0001-90", "Comércio", "Padaria Pão Quente");

        Assert.Equal("Padaria Pão Quente Ltda", empresa.RazaoSocial);
        Assert.Equal("12.345.678/0001-90", empresa.Cnpj);
        Assert.Equal("Comércio", empresa.Segmento);
        Assert.Equal("Padaria Pão Quente", empresa.NomeFantasia);
        Assert.NotEqual(Guid.Empty, empresa.Id);
    }

    [Fact]
    public void NomeExibicao_deve_usar_fantasia_quando_existir()
    {
        var empresa = new Empresa("Padaria Pão Quente Ltda", "12.345.678/0001-90", nomeFantasia: "Padaria Pão Quente");

        Assert.Equal("Padaria Pão Quente", empresa.NomeExibicao);
    }

    [Fact]
    public void NomeExibicao_deve_usar_razao_social_quando_nao_ha_fantasia()
    {
        var empresa = new Empresa("Padaria Pão Quente Ltda", "12.345.678/0001-90");

        Assert.Equal("Padaria Pão Quente Ltda", empresa.NomeExibicao);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Deve_rejeitar_razao_social_vazia(string? razaoSocial)
    {
        Assert.Throws<DomainException>(() => new Empresa(razaoSocial!, "12.345.678/0001-90"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Deve_rejeitar_cnpj_vazio(string? cnpj)
    {
        Assert.Throws<DomainException>(() => new Empresa("Padaria Pão Quente", cnpj!));
    }

    [Theory]
    [InlineData("123")]
    [InlineData("123.456.789-00")]
    [InlineData("12.345.678/0001-900")]
    public void Deve_rejeitar_cnpj_com_quantidade_errada_de_digitos(string cnpj)
    {
        Assert.Throws<DomainException>(() => new Empresa("Padaria Pão Quente", cnpj));
    }
}
