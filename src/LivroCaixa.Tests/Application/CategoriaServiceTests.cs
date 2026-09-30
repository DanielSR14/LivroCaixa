using LivroCaixa.Application.Services;
using LivroCaixa.Domain.Entities;
using LivroCaixa.Domain.Enums;
using LivroCaixa.Domain.Exceptions;
using LivroCaixa.Tests.Application.Fakes;

namespace LivroCaixa.Tests.Application;

public class CategoriaServiceTests
{
    [Fact]
    public async Task Deve_criar_categoria()
    {
        var categorias = new FakeCategoriaRepository();
        var servico = new CategoriaService(categorias, new FakeLancamentoRepository(), new FakeUnitOfWork());
        var empresaId = Guid.NewGuid();

        var categoria = await servico.CriarAsync(empresaId, "Assinaturas", TipoLancamento.Saida);

        Assert.Single(categorias.Categorias);
        Assert.Equal("Assinaturas", categoria.Nome);
        Assert.Equal(TipoLancamento.Saida, categoria.Tipo);
    }

    [Fact]
    public async Task Nao_deve_permitir_categoria_duplicada_no_mesmo_tipo()
    {
        var categorias = new FakeCategoriaRepository();
        var servico = new CategoriaService(categorias, new FakeLancamentoRepository(), new FakeUnitOfWork());
        var empresaId = Guid.NewGuid();

        await servico.CriarAsync(empresaId, "Assinaturas", TipoLancamento.Saida);

        await Assert.ThrowsAsync<DomainException>(() =>
            servico.CriarAsync(empresaId, "assinaturas", TipoLancamento.Saida));
    }

    [Fact]
    public async Task Deve_permitir_mesmo_nome_em_tipos_diferentes()
    {
        var categorias = new FakeCategoriaRepository();
        var servico = new CategoriaService(categorias, new FakeLancamentoRepository(), new FakeUnitOfWork());
        var empresaId = Guid.NewGuid();

        await servico.CriarAsync(empresaId, "Outros", TipoLancamento.Entrada);
        await servico.CriarAsync(empresaId, "Outros", TipoLancamento.Saida);

        Assert.Equal(2, categorias.Categorias.Count);
    }

    [Fact]
    public async Task Deve_atualizar_nome_da_categoria()
    {
        var categorias = new FakeCategoriaRepository();
        var servico = new CategoriaService(categorias, new FakeLancamentoRepository(), new FakeUnitOfWork());
        var empresaId = Guid.NewGuid();
        var categoria = await servico.CriarAsync(empresaId, "Assinaturas", TipoLancamento.Saida);

        await servico.AtualizarAsync(categoria.Id, "Assinaturas de software");

        Assert.Equal("Assinaturas de software", categorias.Categorias.Single().Nome);
    }

    [Fact]
    public async Task Nao_deve_permitir_renomear_para_nome_ja_usado_no_mesmo_tipo()
    {
        var categorias = new FakeCategoriaRepository();
        var servico = new CategoriaService(categorias, new FakeLancamentoRepository(), new FakeUnitOfWork());
        var empresaId = Guid.NewGuid();
        await servico.CriarAsync(empresaId, "Assinaturas", TipoLancamento.Saida);
        var outraCategoria = await servico.CriarAsync(empresaId, "Fornecedores", TipoLancamento.Saida);

        await Assert.ThrowsAsync<DomainException>(() => servico.AtualizarAsync(outraCategoria.Id, "assinaturas"));
    }

    [Fact]
    public async Task Deve_permitir_manter_o_proprio_nome_ao_atualizar()
    {
        var categorias = new FakeCategoriaRepository();
        var servico = new CategoriaService(categorias, new FakeLancamentoRepository(), new FakeUnitOfWork());
        var empresaId = Guid.NewGuid();
        var categoria = await servico.CriarAsync(empresaId, "Assinaturas", TipoLancamento.Saida);

        await servico.AtualizarAsync(categoria.Id, "Assinaturas");

        Assert.Equal("Assinaturas", categorias.Categorias.Single().Nome);
    }

    [Fact]
    public async Task Deve_excluir_categoria_sem_lancamentos()
    {
        var categorias = new FakeCategoriaRepository();
        var servico = new CategoriaService(categorias, new FakeLancamentoRepository(), new FakeUnitOfWork());
        var empresaId = Guid.NewGuid();
        var categoria = await servico.CriarAsync(empresaId, "Assinaturas", TipoLancamento.Saida);

        await servico.ExcluirAsync(categoria.Id);

        Assert.Empty(categorias.Categorias);
    }

    [Fact]
    public async Task Nao_deve_excluir_categoria_com_lancamentos()
    {
        var categorias = new FakeCategoriaRepository();
        var lancamentos = new FakeLancamentoRepository();
        var servico = new CategoriaService(categorias, lancamentos, new FakeUnitOfWork());
        var empresaId = Guid.NewGuid();
        var categoria = await servico.CriarAsync(empresaId, "Assinaturas", TipoLancamento.Saida);
        var conta = new Conta(empresaId, "Caixa", TipoConta.Caixa);
        lancamentos.Lancamentos.Add(new Lancamento(
            empresaId, conta, categoria, TipoLancamento.Saida,
            DateOnly.FromDateTime(DateTime.Today), 50m, FormaPagamento.Pix));

        await Assert.ThrowsAsync<DomainException>(() => servico.ExcluirAsync(categoria.Id));
        Assert.Single(categorias.Categorias);
    }
}
