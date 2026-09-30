using System.Globalization;
using LivroCaixa.Application;
using LivroCaixa.Application.Services;
using LivroCaixa.Domain.Entities;
using LivroCaixa.Domain.Enums;
using LivroCaixa.Domain.Exceptions;
using LivroCaixa.Tests.Application.Fakes;

namespace LivroCaixa.Tests.Application;

/// <summary>
/// Erros clássicos de idioma: vírgula x ponto decimal, acentos, maiúsculas e culturas "difíceis"
/// do Windows (tr-TR, onde "i".ToUpper() é "İ"). O resultado não pode depender da cultura da máquina.
/// </summary>
public class TextoECulturaTests
{
    private static void ComCultura(string cultura, Action acao)
    {
        var anterior = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(cultura);
        try
        {
            acao();
        }
        finally
        {
            CultureInfo.CurrentCulture = anterior;
        }
    }

    [Theory]
    [InlineData("1.500,00", 1500.00)]
    [InlineData("1500,5", 1500.5)]
    [InlineData("0,01", 0.01)]
    [InlineData("1500.50", 1500.50)]
    [InlineData("1500", 1500)]
    [InlineData("R$ 1.234,56", 1234.56)]
    [InlineData("  89,90  ", 89.90)]
    [InlineData("1.234.567,89", 1234567.89)]
    public void ConversorValor_deve_entender_os_formatos_que_o_usuario_digita(string digitado, double esperado)
    {
        foreach (var cultura in new[] { "pt-BR", "en-US", "de-DE", "tr-TR" })
        {
            ComCultura(cultura, () =>
            {
                Assert.True(ConversorValor.TentarLer(digitado, out var valor), $"{digitado} em {cultura}");
                Assert.Equal((decimal)esperado, valor);
            });
        }
    }

    // "1.500" para um brasileiro é mil e quinhentos, não um e meio: lido como decimal, o lançamento
    // ficaria 1000 vezes menor sem nenhum aviso.
    [Theory]
    [InlineData("1.500", 1500)]
    [InlineData("12.000", 12000)]
    [InlineData("1.234.567", 1234567)]
    [InlineData("1.5", 1.5)]
    [InlineData("10.50", 10.50)]
    public void ConversorValor_deve_tratar_ponto_como_milhar_no_padrao_brasileiro(string digitado, double esperado)
    {
        Assert.True(ConversorValor.TentarLer(digitado, out var valor));
        Assert.Equal((decimal)esperado, valor);
    }

    // O formulário de edição preenche o campo com o valor salvo; ler de volta o que foi exibido
    // precisa dar exatamente o mesmo valor (inclusive decimais com escala 3, ex: 1,500 digitado).
    [Theory]
    [InlineData(1500)]
    [InlineData(1.5)]
    [InlineData(0.01)]
    [InlineData(1234567.89)]
    public void Valor_formatado_para_edicao_deve_ser_lido_de_volta_igual(double original)
    {
        foreach (var valorOriginal in new[] { (decimal)original, decimal.Parse(((decimal)original).ToString("0.000", CultureInfo.InvariantCulture), CultureInfo.InvariantCulture) })
        {
            foreach (var cultura in new[] { "pt-BR", "en-US" })
            {
                ComCultura(cultura, () =>
                {
                    var exibido = ConversorValor.Formatar(valorOriginal);
                    Assert.True(ConversorValor.TentarLer(exibido, out var lido), exibido);
                    Assert.Equal(valorOriginal, lido);
                });
            }
        }
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("abc")]
    [InlineData("12,34,56")]
    [InlineData(null)]
    public void ConversorValor_deve_recusar_texto_que_nao_e_valor(string? digitado)
    {
        Assert.False(ConversorValor.TentarLer(digitado, out _));
    }

    [Fact]
    public void ConversorValor_opcional_deve_aceitar_vazio_como_sem_valor()
    {
        Assert.True(ConversorValor.TentarLerOpcional("", out var vazio));
        Assert.Null(vazio);
        Assert.True(ConversorValor.TentarLerOpcional("10,50", out var preenchido));
        Assert.Equal(10.50m, preenchido);
        Assert.False(ConversorValor.TentarLerOpcional("dez", out _));
    }

    [Theory]
    [InlineData("Conta de Água", "agua")]
    [InlineData("Ação social", "ACAO")]
    [InlineData("Taxa de São João", "sao joao")]
    [InlineData("iptu 2026", "IPTU")]
    [InlineData("Manutenção", "manutenc")]
    public void Busca_deve_ignorar_maiusculas_e_acentos_em_qualquer_cultura(string texto, string busca)
    {
        foreach (var cultura in new[] { "pt-BR", "tr-TR", "en-US" })
            ComCultura(cultura, () => Assert.True(BuscaTexto.Contem(texto, busca), $"{busca} em {cultura}"));
    }

    [Fact]
    public void Busca_nao_deve_achar_o_que_nao_esta_no_texto()
    {
        Assert.False(BuscaTexto.Contem("Aluguel da sede", "luz"));
        Assert.False(BuscaTexto.Contem(null, "luz"));
    }

    [Fact]
    public void Codigos_digitados_devem_virar_ascii_mesmo_no_windows_em_turco()
    {
        ComCultura("tr-TR", () =>
        {
            Assert.Equal("ALI", ModeloLancamento.NormalizarCodigo(" ali "));
            Assert.Equal("ABCDEFGHIJKM", CodigoRecuperacao.Normalizar("abcd efgh-ijkm"));
        });
    }

    [Theory]
    [InlineData("ÁGUA")]
    [InlineData("água")]
    [InlineData("  Água  ")]
    public async Task Categoria_duplicada_deve_ser_detectada_ignorando_maiusculas_e_espacos(string nomeRepetido)
    {
        var servico = new CategoriaService(new FakeCategoriaRepository(), new FakeLancamentoRepository(), new FakeUnitOfWork());
        var empresaId = Guid.NewGuid();
        await servico.CriarAsync(empresaId, "Água", TipoLancamento.Saida);

        await Assert.ThrowsAsync<DomainException>(() => servico.CriarAsync(empresaId, nomeRepetido, TipoLancamento.Saida));
    }
}
