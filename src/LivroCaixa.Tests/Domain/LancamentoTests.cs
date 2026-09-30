using LivroCaixa.Domain.Entities;
using LivroCaixa.Domain.Enums;
using LivroCaixa.Domain.Exceptions;

namespace LivroCaixa.Tests.Domain;

public class LancamentoTests
{
    private static (Guid EmpresaId, Conta Conta, Categoria CategoriaEntrada, Categoria CategoriaSaida) CriarCenario()
    {
        var empresaId = Guid.NewGuid();
        var conta = new Conta(empresaId, "Caixa", TipoConta.Caixa);
        var categoriaEntrada = new Categoria(empresaId, "Serviços", TipoLancamento.Entrada);
        var categoriaSaida = new Categoria(empresaId, "Fornecedores", TipoLancamento.Saida);

        return (empresaId, conta, categoriaEntrada, categoriaSaida);
    }

    [Fact]
    public void Deve_criar_lancamento_de_entrada_valido()
    {
        var (empresaId, conta, categoriaEntrada, _) = CriarCenario();

        var lancamento = new Lancamento(
            empresaId,
            conta,
            categoriaEntrada,
            TipoLancamento.Entrada,
            DateOnly.FromDateTime(DateTime.Today),
            1250m,
            FormaPagamento.Pix,
            "Pagamento referente ao serviço X");

        Assert.Equal(1250m, lancamento.Valor);
        Assert.Equal(TipoLancamento.Entrada, lancamento.Tipo);
        Assert.Equal(conta.Id, lancamento.ContaId);
        Assert.Equal(categoriaEntrada.Id, lancamento.CategoriaId);
        Assert.Null(lancamento.AlteradoEm);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void Deve_rejeitar_valor_zero_ou_negativo(decimal valor)
    {
        var (empresaId, conta, categoriaEntrada, _) = CriarCenario();

        Assert.Throws<DomainException>(() => new Lancamento(
            empresaId,
            conta,
            categoriaEntrada,
            TipoLancamento.Entrada,
            DateOnly.FromDateTime(DateTime.Today),
            valor,
            FormaPagamento.Dinheiro));
    }

    [Fact]
    public void Deve_rejeitar_data_futura()
    {
        var (empresaId, conta, categoriaEntrada, _) = CriarCenario();
        var dataFutura = DateOnly.FromDateTime(DateTime.Today).AddDays(1);

        Assert.Throws<DomainException>(() => new Lancamento(
            empresaId,
            conta,
            categoriaEntrada,
            TipoLancamento.Entrada,
            dataFutura,
            100m,
            FormaPagamento.Dinheiro));
    }

    [Fact]
    public void Deve_rejeitar_categoria_de_tipo_diferente_do_lancamento()
    {
        var (empresaId, conta, _, categoriaSaida) = CriarCenario();

        Assert.Throws<DomainException>(() => new Lancamento(
            empresaId,
            conta,
            categoriaSaida,
            TipoLancamento.Entrada,
            DateOnly.FromDateTime(DateTime.Today),
            100m,
            FormaPagamento.Dinheiro));
    }

    [Fact]
    public void Deve_rejeitar_conta_de_outra_empresa()
    {
        var (empresaId, _, categoriaEntrada, _) = CriarCenario();
        var contaDeOutraEmpresa = new Conta(Guid.NewGuid(), "Caixa", TipoConta.Caixa);

        Assert.Throws<DomainException>(() => new Lancamento(
            empresaId,
            contaDeOutraEmpresa,
            categoriaEntrada,
            TipoLancamento.Entrada,
            DateOnly.FromDateTime(DateTime.Today),
            100m,
            FormaPagamento.Dinheiro));
    }

    [Fact]
    public void Deve_atualizar_lancamento_e_marcar_data_de_alteracao()
    {
        var (empresaId, conta, categoriaEntrada, _) = CriarCenario();
        var lancamento = new Lancamento(
            empresaId,
            conta,
            categoriaEntrada,
            TipoLancamento.Entrada,
            DateOnly.FromDateTime(DateTime.Today),
            100m,
            FormaPagamento.Dinheiro);

        lancamento.Atualizar(
            conta,
            categoriaEntrada,
            DateOnly.FromDateTime(DateTime.Today),
            200m,
            FormaPagamento.Pix,
            "Novo valor",
            null);

        Assert.Equal(200m, lancamento.Valor);
        Assert.Equal(FormaPagamento.Pix, lancamento.FormaPagamento);
        Assert.NotNull(lancamento.AlteradoEm);
    }

    [Fact]
    public void Deve_adicionar_anexo_pertencente_ao_lancamento()
    {
        var (empresaId, conta, categoriaEntrada, _) = CriarCenario();
        var lancamento = new Lancamento(
            empresaId,
            conta,
            categoriaEntrada,
            TipoLancamento.Entrada,
            DateOnly.FromDateTime(DateTime.Today),
            100m,
            FormaPagamento.Dinheiro);

        var anexo = new Anexo(lancamento.Id, "abc123.pdf", "nota.pdf", TipoArquivoAnexo.Pdf, 1024);
        lancamento.AdicionarAnexo(anexo);

        Assert.Single(lancamento.Anexos);
    }

    [Fact]
    public void Deve_rejeitar_anexo_de_outro_lancamento()
    {
        var (empresaId, conta, categoriaEntrada, _) = CriarCenario();
        var lancamento = new Lancamento(
            empresaId,
            conta,
            categoriaEntrada,
            TipoLancamento.Entrada,
            DateOnly.FromDateTime(DateTime.Today),
            100m,
            FormaPagamento.Dinheiro);

        var anexoDeOutroLancamento = new Anexo(Guid.NewGuid(), "abc123.pdf", "nota.pdf", TipoArquivoAnexo.Pdf, 1024);

        Assert.Throws<DomainException>(() => lancamento.AdicionarAnexo(anexoDeOutroLancamento));
    }

    [Fact]
    public void Deve_permitir_excluir_lancamento_do_mes_atual()
    {
        var (empresaId, conta, categoriaEntrada, _) = CriarCenario();
        var lancamento = new Lancamento(
            empresaId,
            conta,
            categoriaEntrada,
            TipoLancamento.Entrada,
            DateOnly.FromDateTime(DateTime.Today),
            100m,
            FormaPagamento.Dinheiro);

        lancamento.Excluir();

        Assert.NotNull(lancamento.ExcluidoEm);
    }

    [Fact]
    public void Deve_rejeitar_exclusao_de_lancamento_de_mes_anterior()
    {
        var (empresaId, conta, categoriaEntrada, _) = CriarCenario();
        var mesPassado = new DateOnly(DateTime.Today.Year, DateTime.Today.Month, 1).AddDays(-1);
        var lancamento = new Lancamento(
            empresaId,
            conta,
            categoriaEntrada,
            TipoLancamento.Entrada,
            mesPassado,
            100m,
            FormaPagamento.Dinheiro);

        Assert.Throws<DomainException>(() => lancamento.Excluir());
        Assert.Null(lancamento.ExcluidoEm);
    }
}
