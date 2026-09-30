using LivroCaixa.Application;
using LivroCaixa.Application.DTOs;
using LivroCaixa.Application.Services;
using LivroCaixa.Domain.Entities;
using LivroCaixa.Domain.Enums;
using LivroCaixa.Tests.Application.Fakes;

namespace LivroCaixa.Tests.Application;

/// <summary>Histórico mostrando só o que mudou em cada edição, com o valor de antes e o de depois.</summary>
public class HistoricoAlteracoesTests
{
    private readonly Guid _empresaId = Guid.NewGuid();
    private readonly Conta _caixa;
    private readonly Conta _banco;
    private readonly Categoria _mensalidades;
    private readonly Categoria _doacoes;
    private readonly FakeLancamentoRepository _lancamentos = new();
    private readonly LancamentoService _servico;
    private readonly DateOnly _hoje = DateOnly.FromDateTime(DateTime.Today);

    public HistoricoAlteracoesTests()
    {
        _caixa = new Conta(_empresaId, "Caixa", TipoConta.Caixa);
        _banco = new Conta(_empresaId, "Sicoob", TipoConta.Banco);
        _mensalidades = new Categoria(_empresaId, "Mensalidades", TipoLancamento.Entrada);
        _doacoes = new Categoria(_empresaId, "Doações", TipoLancamento.Entrada);

        var contas = new FakeContaRepository();
        contas.Contas.AddRange(new[] { _caixa, _banco });
        var categorias = new FakeCategoriaRepository();
        categorias.Categorias.AddRange(new[] { _mensalidades, _doacoes });

        _servico = new LancamentoService(_lancamentos, contas, categorias, new FakeTransferenciaRepository(), new FakeUnitOfWork());
    }

    private Task<Lancamento> CriarAsync() =>
        _servico.RegistrarAsync(_empresaId, new NovoLancamentoDto(
            _caixa.Id, _mensalidades.Id, TipoLancamento.Entrada, _hoje, 100m, FormaPagamento.Pix, "Mensalidade de março", null));

    private Task EditarAsync(Guid id, Conta conta, Categoria categoria, decimal valor, string? descricao, FormaPagamento forma = FormaPagamento.Pix) =>
        _servico.AtualizarAsync(new AtualizarLancamentoDto(id, conta.Id, categoria.Id, _hoje, valor, forma, descricao, null));

    [Fact]
    public async Task Edicao_deve_listar_somente_os_campos_que_mudaram_com_antes_e_depois()
    {
        var lancamento = await CriarAsync();

        await EditarAsync(lancamento.Id, _banco, _mensalidades, 150m, "Mensalidade de março");

        var edicao = Assert.Single(await _servico.ListarHistoricoAsync(lancamento.Id), h => h.TipoEvento == TipoEventoHistorico.Editado);
        Assert.Equal(new[]
        {
            new AlteracaoCampoDto("Valor", "R$ 100,00", "R$ 150,00"),
            new AlteracaoCampoDto("Conta", "Caixa", "Sicoob")
        }, edicao.Alteracoes);
    }

    [Fact]
    public async Task Cada_edicao_deve_mostrar_o_proprio_depois_e_nao_o_estado_final()
    {
        var lancamento = await CriarAsync();
        await EditarAsync(lancamento.Id, _caixa, _mensalidades, 150m, "Mensalidade de março");
        await EditarAsync(lancamento.Id, _caixa, _doacoes, 150m, "Doação avulsa", FormaPagamento.Dinheiro);

        var edicoes = (await _servico.ListarHistoricoAsync(lancamento.Id))
            .Where(h => h.TipoEvento == TipoEventoHistorico.Editado)
            .ToList();

        // Mais recente primeiro.
        Assert.Equal(new[]
        {
            new AlteracaoCampoDto("Categoria", "Mensalidades", "Doações"),
            new AlteracaoCampoDto("Forma de pagamento", "Pix", "Dinheiro"),
            new AlteracaoCampoDto("Descrição", "Mensalidade de março", "Doação avulsa")
        }, edicoes[0].Alteracoes);
        Assert.Equal(new[] { new AlteracaoCampoDto("Valor", "R$ 100,00", "R$ 150,00") }, edicoes[1].Alteracoes);
    }

    [Fact]
    public async Task Edicao_seguida_de_exclusao_deve_usar_o_retrato_da_exclusao_como_depois()
    {
        var lancamento = await CriarAsync();
        await EditarAsync(lancamento.Id, _caixa, _mensalidades, 120m, null);
        await _servico.ExcluirAsync(lancamento.Id);

        var historico = await _servico.ListarHistoricoAsync(lancamento.Id);

        Assert.Equal(TipoEventoHistorico.Excluido, historico[0].TipoEvento);
        Assert.Empty(historico[0].Alteracoes);
        var edicao = Assert.Single(historico, h => h.TipoEvento == TipoEventoHistorico.Editado);
        Assert.Equal(new[]
        {
            new AlteracaoCampoDto("Valor", "R$ 100,00", "R$ 120,00"),
            new AlteracaoCampoDto("Descrição", "Mensalidade de março", "(vazio)")
        }, edicao.Alteracoes);
    }

    [Fact]
    public async Task Salvar_sem_mudar_nada_deve_registrar_edicao_sem_alteracoes()
    {
        var lancamento = await CriarAsync();

        await EditarAsync(lancamento.Id, _caixa, _mensalidades, 100m, "Mensalidade de março");

        Assert.Empty(Assert.Single(await _servico.ListarHistoricoAsync(lancamento.Id), h => h.TipoEvento == TipoEventoHistorico.Editado).Alteracoes);
    }

    // Retratos gravados antes de 2026-09-29 não têm Conta/Categoria e trazem o enum como número.
    [Fact]
    public void Retrato_antigo_sem_conta_e_categoria_deve_comparar_so_os_campos_que_existem()
    {
        var antigo = RetratoLancamento.Ler("{\"Data\":\"2026-09-01\",\"Valor\":100,\"FormaPagamento\":1,\"Descricao\":null,\"Observacoes\":null}")!;
        var atual = RetratoLancamento.Ler("{\"Data\":\"2026-09-02\",\"Valor\":100.0,\"FormaPagamento\":1,\"Descricao\":\"x\",\"Observacoes\":null,\"Conta\":\"Caixa\",\"Categoria\":\"Aluguel\"}")!;

        Assert.Equal(new[]
        {
            new AlteracaoCampoDto("Data", "01/09/2026", "02/09/2026"),
            new AlteracaoCampoDto("Descrição", "(vazio)", "x")
        }, RetratoLancamento.Comparar(antigo, atual));
    }

    [Theory]
    [InlineData("não é json")]
    [InlineData("[1,2,3]")]
    [InlineData("{\"Data\":\"01/09/2026\"}")]
    [InlineData("")]
    [InlineData(null)]
    public void Retrato_estragado_nao_deve_derrubar_a_tela(string? json)
    {
        var excecao = Record.Exception(() => RetratoLancamento.Ler(json));

        Assert.Null(excecao);
    }
}
