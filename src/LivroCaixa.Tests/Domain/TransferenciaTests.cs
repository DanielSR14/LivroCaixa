using LivroCaixa.Domain.Entities;
using LivroCaixa.Domain.Enums;
using LivroCaixa.Domain.Exceptions;

namespace LivroCaixa.Tests.Domain;

public class TransferenciaTests
{
    private readonly Guid _empresaId = Guid.NewGuid();
    private readonly Conta _banco;
    private readonly Conta _caixa;
    private readonly DateOnly _hoje = DateOnly.FromDateTime(DateTime.Today);

    public TransferenciaTests()
    {
        _banco = new Conta(_empresaId, "Banco", TipoConta.Banco);
        _caixa = new Conta(_empresaId, "Caixa", TipoConta.Caixa);
    }

    [Fact]
    public void Nao_deve_permitir_mesma_conta_valor_invalido_ou_data_futura()
    {
        Assert.Throws<DomainException>(() => new Transferencia(_empresaId, _banco, _banco, _hoje, 10m));
        Assert.Throws<DomainException>(() => new Transferencia(_empresaId, _banco, _caixa, _hoje, 0m));
        Assert.Throws<DomainException>(() => new Transferencia(_empresaId, _banco, _caixa, _hoje.AddDays(2), 10m));
    }

    [Fact]
    public void Nao_deve_permitir_conta_de_outra_empresa()
    {
        var contaDeOutraEmpresa = new Conta(Guid.NewGuid(), "Banco", TipoConta.Banco);

        Assert.Throws<DomainException>(() => new Transferencia(_empresaId, _banco, contaDeOutraEmpresa, _hoje, 10m));
    }

    [Fact]
    public void Nao_deve_excluir_transferencia_de_mes_anterior()
    {
        var mesPassado = new DateOnly(DateTime.Today.Year, DateTime.Today.Month, 1).AddDays(-1);
        var transferencia = new Transferencia(_empresaId, _banco, _caixa, mesPassado, 10m);

        Assert.Throws<DomainException>(() => transferencia.Excluir());
    }

    [Fact]
    public void Deve_excluir_transferencia_do_mes_atual_uma_unica_vez()
    {
        var transferencia = new Transferencia(_empresaId, _banco, _caixa, _hoje, 10m);

        transferencia.Excluir();

        Assert.NotNull(transferencia.ExcluidoEm);
        Assert.Throws<DomainException>(() => transferencia.Excluir());
    }
}
