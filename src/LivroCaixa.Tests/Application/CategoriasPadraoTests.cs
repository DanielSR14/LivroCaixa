using LivroCaixa.Application.Services;
using LivroCaixa.Domain.Enums;

namespace LivroCaixa.Tests.Application;

public class CategoriasPadraoTests
{
    [Theory]
    [InlineData(CategoriasPadrao.Segmentos.AssociacaoOuSindicato)]
    [InlineData(CategoriasPadrao.Segmentos.Comercio)]
    [InlineData(CategoriasPadrao.Segmentos.Servicos)]
    [InlineData(CategoriasPadrao.Segmentos.Industria)]
    [InlineData(CategoriasPadrao.Segmentos.Outro)]
    [InlineData(null)]
    public void Deve_gerar_categorias_de_entrada_e_saida_para_qualquer_segmento(string? segmento)
    {
        var empresaId = Guid.NewGuid();

        var categorias = CategoriasPadrao.Criar(empresaId, segmento);

        Assert.All(categorias, c => Assert.Equal(empresaId, c.EmpresaId));
        Assert.Contains(categorias, c => c.Tipo == TipoLancamento.Entrada);
        Assert.Contains(categorias, c => c.Tipo == TipoLancamento.Saida);
    }

    [Fact]
    public void Segmentos_diferentes_devem_gerar_conjuntos_diferentes()
    {
        var empresaId = Guid.NewGuid();

        var comercio = CategoriasPadrao.Criar(empresaId, CategoriasPadrao.Segmentos.Comercio).Select(c => c.Nome).ToList();
        var servicos = CategoriasPadrao.Criar(empresaId, CategoriasPadrao.Segmentos.Servicos).Select(c => c.Nome).ToList();

        Assert.NotEqual(comercio, servicos);
    }

    [Fact]
    public void Segmento_nao_reconhecido_deve_cair_no_modelo_generico()
    {
        var empresaId = Guid.NewGuid();

        var generico = CategoriasPadrao.Criar(empresaId, null).Select(c => c.Nome).ToList();
        var desconhecido = CategoriasPadrao.Criar(empresaId, "Segmento inexistente").Select(c => c.Nome).ToList();

        Assert.Equal(generico, desconhecido);
    }
}
