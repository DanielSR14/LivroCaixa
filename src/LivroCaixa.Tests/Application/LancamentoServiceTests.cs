using LivroCaixa.Application.DTOs;
using LivroCaixa.Application.Services;
using LivroCaixa.Domain.Entities;
using LivroCaixa.Domain.Enums;
using LivroCaixa.Domain.Exceptions;
using LivroCaixa.Tests.Application.Fakes;

namespace LivroCaixa.Tests.Application;

public class LancamentoServiceTests
{
    private sealed class Cenario
    {
        public Guid EmpresaId { get; } = Guid.NewGuid();
        public Conta Conta { get; }
        public Categoria CategoriaEntrada { get; }
        public Categoria CategoriaSaida { get; }
        public FakeLancamentoRepository Lancamentos { get; } = new();
        public FakeContaRepository Contas { get; } = new();
        public FakeCategoriaRepository Categorias { get; } = new();
        public FakeUnitOfWork UnitOfWork { get; } = new();
        public LancamentoService Servico { get; }

        public Cenario()
        {
            Conta = new Conta(EmpresaId, "Caixa", TipoConta.Caixa, 100m);
            CategoriaEntrada = new Categoria(EmpresaId, "Serviços", TipoLancamento.Entrada);
            CategoriaSaida = new Categoria(EmpresaId, "Fornecedores", TipoLancamento.Saida);

            Contas.Contas.Add(Conta);
            Categorias.Categorias.Add(CategoriaEntrada);
            Categorias.Categorias.Add(CategoriaSaida);

            Servico = new LancamentoService(Lancamentos, Contas, Categorias, new FakeTransferenciaRepository(), UnitOfWork);
        }
    }

    [Fact]
    public async Task Deve_registrar_lancamento_e_salvar()
    {
        var cenario = new Cenario();
        var dto = new NovoLancamentoDto(
            cenario.Conta.Id, cenario.CategoriaEntrada.Id, TipoLancamento.Entrada,
            DateOnly.FromDateTime(DateTime.Today), 300m, FormaPagamento.Pix, "Serviço X", null);

        var lancamento = await cenario.Servico.RegistrarAsync(cenario.EmpresaId, dto);

        Assert.Single(cenario.Lancamentos.Lancamentos);
        Assert.Equal(300m, lancamento.Valor);
        Assert.Equal(1, cenario.UnitOfWork.VezesSalvo);
    }

    [Fact]
    public async Task Deve_atualizar_lancamento_existente()
    {
        var cenario = new Cenario();
        var lancamento = await cenario.Servico.RegistrarAsync(cenario.EmpresaId, new NovoLancamentoDto(
            cenario.Conta.Id, cenario.CategoriaEntrada.Id, TipoLancamento.Entrada,
            DateOnly.FromDateTime(DateTime.Today), 300m, FormaPagamento.Pix, null, null));

        await cenario.Servico.AtualizarAsync(new AtualizarLancamentoDto(
            lancamento.Id, cenario.Conta.Id, cenario.CategoriaEntrada.Id,
            DateOnly.FromDateTime(DateTime.Today), 500m, FormaPagamento.Dinheiro, "Atualizado", null));

        var detalhe = await cenario.Servico.ObterDetalheAsync(lancamento.Id);
        Assert.Equal(500m, detalhe.Valor);
        Assert.Equal("Atualizado", detalhe.Descricao);
    }

    [Fact]
    public async Task Deve_excluir_lancamento_por_exclusao_logica()
    {
        var cenario = new Cenario();
        var lancamento = await cenario.Servico.RegistrarAsync(cenario.EmpresaId, new NovoLancamentoDto(
            cenario.Conta.Id, cenario.CategoriaEntrada.Id, TipoLancamento.Entrada,
            DateOnly.FromDateTime(DateTime.Today), 300m, FormaPagamento.Pix, null, null));

        await cenario.Servico.ExcluirAsync(lancamento.Id);

        // Continua existindo na base (exclusão lógica), mas não aparece mais no extrato ativo.
        Assert.Single(cenario.Lancamentos.Lancamentos);
        Assert.NotNull(lancamento.ExcluidoEm);
        var extrato = await cenario.Servico.ListarExtratoConsolidadoAsync(cenario.EmpresaId);
        Assert.Empty(extrato);
        var excluidos = await cenario.Servico.ListarExcluidosAsync(cenario.EmpresaId);
        Assert.Single(excluidos);
    }

    [Fact]
    public async Task Deve_restaurar_lancamento_excluido()
    {
        var cenario = new Cenario();
        var lancamento = await cenario.Servico.RegistrarAsync(cenario.EmpresaId, new NovoLancamentoDto(
            cenario.Conta.Id, cenario.CategoriaEntrada.Id, TipoLancamento.Entrada,
            DateOnly.FromDateTime(DateTime.Today), 300m, FormaPagamento.Pix, null, null));
        await cenario.Servico.ExcluirAsync(lancamento.Id);

        await cenario.Servico.RestaurarAsync(lancamento.Id);

        Assert.Null(lancamento.ExcluidoEm);
        var extrato = await cenario.Servico.ListarExtratoConsolidadoAsync(cenario.EmpresaId);
        Assert.Single(extrato);
    }

    [Fact]
    public async Task Deve_registrar_historico_de_criacao_edicao_e_exclusao()
    {
        var cenario = new Cenario();
        var lancamento = await cenario.Servico.RegistrarAsync(cenario.EmpresaId, new NovoLancamentoDto(
            cenario.Conta.Id, cenario.CategoriaEntrada.Id, TipoLancamento.Entrada,
            DateOnly.FromDateTime(DateTime.Today), 300m, FormaPagamento.Pix, null, null));

        await cenario.Servico.AtualizarAsync(new AtualizarLancamentoDto(
            lancamento.Id, cenario.Conta.Id, cenario.CategoriaEntrada.Id,
            DateOnly.FromDateTime(DateTime.Today), 500m, FormaPagamento.Dinheiro, "Atualizado", null));
        await cenario.Servico.ExcluirAsync(lancamento.Id);

        var historico = await cenario.Servico.ListarHistoricoAsync(lancamento.Id);

        Assert.Equal(3, historico.Count);
        Assert.Contains(historico, h => h.TipoEvento == TipoEventoHistorico.Criado && h.DadosAnteriores == null);
        Assert.Contains(historico, h => h.TipoEvento == TipoEventoHistorico.Editado && h.DadosAnteriores != null);
        Assert.Contains(historico, h => h.TipoEvento == TipoEventoHistorico.Excluido && h.DadosAnteriores != null);
    }

    [Fact]
    public async Task Deve_lancar_erro_ao_excluir_lancamento_inexistente()
    {
        var cenario = new Cenario();

        await Assert.ThrowsAsync<DomainException>(() => cenario.Servico.ExcluirAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task Deve_montar_extrato_com_saldo_acumulado_em_ordem_decrescente_de_data()
    {
        var cenario = new Cenario();
        var hoje = DateOnly.FromDateTime(DateTime.Today);

        await cenario.Servico.RegistrarAsync(cenario.EmpresaId, new NovoLancamentoDto(
            cenario.Conta.Id, cenario.CategoriaEntrada.Id, TipoLancamento.Entrada, hoje.AddDays(-1), 300m, FormaPagamento.Pix, "Primeiro", null));
        await cenario.Servico.RegistrarAsync(cenario.EmpresaId, new NovoLancamentoDto(
            cenario.Conta.Id, cenario.CategoriaSaida.Id, TipoLancamento.Saida, hoje, 80m, FormaPagamento.Dinheiro, "Segundo", null));

        var extrato = await cenario.Servico.ListarExtratoConsolidadoAsync(cenario.EmpresaId);

        Assert.Equal(2, extrato.Count);
        Assert.Equal("Segundo", extrato[0].Descricao);
        Assert.Equal(320m, extrato[0].SaldoAposLancamento);
        Assert.Equal("Caixa", extrato[0].Conta);
        Assert.Equal("Primeiro", extrato[1].Descricao);
        Assert.Equal(400m, extrato[1].SaldoAposLancamento);
    }

    [Fact]
    public async Task Deve_obter_saldo_total_somando_todas_as_contas()
    {
        var cenario = new Cenario();
        var outraConta = new Conta(cenario.EmpresaId, "Banco", TipoConta.Banco, 1000m);
        cenario.Contas.Contas.Add(outraConta);

        await cenario.Servico.RegistrarAsync(cenario.EmpresaId, new NovoLancamentoDto(
            cenario.Conta.Id, cenario.CategoriaEntrada.Id, TipoLancamento.Entrada,
            DateOnly.FromDateTime(DateTime.Today), 300m, FormaPagamento.Pix, null, null));
        await cenario.Servico.RegistrarAsync(cenario.EmpresaId, new NovoLancamentoDto(
            outraConta.Id, cenario.CategoriaSaida.Id, TipoLancamento.Saida,
            DateOnly.FromDateTime(DateTime.Today), 200m, FormaPagamento.Dinheiro, null, null));

        var saldoTotal = await cenario.Servico.ObterSaldoTotalAsync(cenario.EmpresaId);

        // Caixa: 100 (inicial) + 300 (entrada) = 400. Banco: 1000 (inicial) - 200 (saída) = 800.
        Assert.Equal(1200m, saldoTotal);
    }

    [Fact]
    public async Task Extrato_consolidado_deve_incluir_lancamentos_de_todas_as_contas()
    {
        var cenario = new Cenario();
        var outraConta = new Conta(cenario.EmpresaId, "Banco", TipoConta.Banco);
        cenario.Contas.Contas.Add(outraConta);

        await cenario.Servico.RegistrarAsync(cenario.EmpresaId, new NovoLancamentoDto(
            cenario.Conta.Id, cenario.CategoriaEntrada.Id, TipoLancamento.Entrada,
            DateOnly.FromDateTime(DateTime.Today), 300m, FormaPagamento.Pix, "Do caixa", null));
        await cenario.Servico.RegistrarAsync(cenario.EmpresaId, new NovoLancamentoDto(
            outraConta.Id, cenario.CategoriaEntrada.Id, TipoLancamento.Entrada,
            DateOnly.FromDateTime(DateTime.Today), 500m, FormaPagamento.Pix, "Do banco", null));

        var extrato = await cenario.Servico.ListarExtratoConsolidadoAsync(cenario.EmpresaId);

        Assert.Equal(2, extrato.Count);
        Assert.Contains(extrato, l => l.Descricao == "Do caixa" && l.Conta == "Caixa");
        Assert.Contains(extrato, l => l.Descricao == "Do banco" && l.Conta == "Banco");
    }
}
