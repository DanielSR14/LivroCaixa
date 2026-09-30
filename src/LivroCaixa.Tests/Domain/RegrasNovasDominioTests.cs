using LivroCaixa.Domain.Entities;
using LivroCaixa.Domain.Enums;
using LivroCaixa.Domain.Exceptions;

namespace LivroCaixa.Tests.Domain;

/// <summary>Regras de Domain das entidades de modelo, remessa, bloqueio/termo e código de recuperação.</summary>
public class RegrasNovasDominioTests
{
    private readonly Guid _empresaId = Guid.NewGuid();
    private readonly Categoria _aluguel;
    private readonly Categoria _mensalidades;

    public RegrasNovasDominioTests()
    {
        _aluguel = new Categoria(_empresaId, "Aluguel", TipoLancamento.Saida);
        _mensalidades = new Categoria(_empresaId, "Mensalidades", TipoLancamento.Entrada);
    }

    private ModeloLancamento Modelo(decimal? valor = 100m, string? descricao = null, Categoria? categoria = null, Conta? conta = null) =>
        new(_empresaId, "ALU", "Aluguel da sede", categoria ?? _aluguel, conta, FormaPagamento.Boleto, valor, descricao);

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void Modelo_nao_aceita_valor_padrao_zero_ou_negativo(decimal valor)
    {
        Assert.Throws<DomainException>(() => Modelo(valor));
    }

    [Fact]
    public void Modelo_sem_valor_e_descricao_em_branco_ficam_vazios()
    {
        var modelo = Modelo(valor: null, descricao: "   ");

        Assert.Null(modelo.Valor);
        Assert.Null(modelo.Descricao);
    }

    [Fact]
    public void Modelo_nao_aceita_categoria_ou_conta_de_outra_empresa()
    {
        var categoriaDeFora = new Categoria(Guid.NewGuid(), "Aluguel", TipoLancamento.Saida);
        var contaDeFora = new Conta(Guid.NewGuid(), "Caixa", TipoConta.Caixa);

        Assert.Throws<DomainException>(() => Modelo(categoria: categoriaDeFora));
        Assert.Throws<DomainException>(() => Modelo(conta: contaDeFora));
    }

    [Fact]
    public void Tipo_do_modelo_acompanha_a_categoria_ao_editar()
    {
        var modelo = Modelo();

        modelo.Atualizar("MENS", "Mensalidade", _mensalidades, null, FormaPagamento.Pix, null, null);

        Assert.Equal(TipoLancamento.Entrada, modelo.Tipo);
        Assert.Equal("MENS", modelo.Codigo);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(13)]
    public void Remessa_nao_aceita_mes_invalido(int mes)
    {
        Assert.Throws<DomainException>(() => new RemessaExportacao(_empresaId, 2026, mes, 1, @"C:\x.contabil"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(5)]
    [InlineData(60)]
    public void Empresa_aceita_os_tempos_de_bloqueio_oferecidos(int minutos)
    {
        var empresa = new Empresa("Sindicato", "12.345.678/0001-90");

        empresa.DefinirBloqueioInatividade(minutos);

        Assert.Equal(minutos, empresa.MinutosBloqueioInatividade);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(7)]
    [InlineData(1440)]
    public void Empresa_recusa_tempo_de_bloqueio_fora_das_opcoes(int minutos)
    {
        var empresa = new Empresa("Sindicato", "12.345.678/0001-90");

        Assert.Throws<DomainException>(() => empresa.DefinirBloqueioInatividade(minutos));
        Assert.Equal(Empresa.BloqueioInatividadePadraoMinutos, empresa.MinutosBloqueioInatividade);
    }

    [Fact]
    public void Aceite_do_termo_vale_so_para_a_versao_aceita()
    {
        var empresa = new Empresa("Sindicato", "12.345.678/0001-90");
        Assert.False(empresa.AceitouTermo("1"));

        empresa.AceitarTermo(" 1 ");

        Assert.True(empresa.AceitouTermo("1"));
        Assert.False(empresa.AceitouTermo("2"));
        Assert.NotNull(empresa.TermoAceitoEm);
        Assert.Throws<DomainException>(() => empresa.AceitarTermo(""));
    }

    [Fact]
    public void Usuario_nasce_sem_codigo_de_recuperacao_e_nao_aceita_codigo_vazio()
    {
        var usuario = new Usuario(_empresaId, "tesoureiro", "hash", PerfilUsuario.Administrador);

        Assert.False(usuario.TemCodigoRecuperacao);
        Assert.Throws<DomainException>(() => usuario.DefinirCodigoRecuperacao(" "));

        usuario.DefinirCodigoRecuperacao("hash-do-codigo");
        Assert.True(usuario.TemCodigoRecuperacao);
    }

    [Fact]
    public void Usuario_deve_guardar_o_nome_sem_espacos_nas_pontas()
    {
        Assert.Equal("João Silva", new Usuario(_empresaId, "  João Silva ", "hash", PerfilUsuario.Administrador).Nome);
    }

    [Fact]
    public void Lancamento_excluido_nao_pode_ser_excluido_de_novo_nem_restaurado_sem_estar_excluido()
    {
        var conta = new Conta(_empresaId, "Caixa", TipoConta.Caixa);
        var lancamento = new Lancamento(_empresaId, conta, _mensalidades, TipoLancamento.Entrada,
            DateOnly.FromDateTime(DateTime.Today), 10m, FormaPagamento.Pix);

        Assert.Throws<DomainException>(() => lancamento.Restaurar());
        lancamento.Excluir();
        Assert.Throws<DomainException>(() => lancamento.Excluir());
        lancamento.Restaurar();
        Assert.Null(lancamento.ExcluidoEm);
    }
}
