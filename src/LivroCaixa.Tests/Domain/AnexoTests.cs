using LivroCaixa.Domain.Entities;
using LivroCaixa.Domain.Enums;
using LivroCaixa.Domain.Exceptions;

namespace LivroCaixa.Tests.Domain;

public class AnexoTests
{
    [Fact]
    public void Deve_criar_anexo_com_dados_validos()
    {
        var lancamentoId = Guid.NewGuid();

        var anexo = new Anexo(lancamentoId, "abc123.pdf", "nota.pdf", TipoArquivoAnexo.Pdf, 2048);

        Assert.Equal(lancamentoId, anexo.LancamentoId);
        Assert.Equal("nota.pdf", anexo.NomeOriginal);
        Assert.Equal(TipoArquivoAnexo.Pdf, anexo.TipoArquivo);
        Assert.Equal(2048, anexo.TamanhoBytes);
    }

    [Fact]
    public void Deve_rejeitar_nome_original_vazio()
    {
        Assert.Throws<DomainException>(() =>
            new Anexo(Guid.NewGuid(), "abc123.pdf", "", TipoArquivoAnexo.Pdf, 2048));
    }

    [Fact]
    public void Deve_rejeitar_arquivo_acima_do_tamanho_maximo()
    {
        var tamanhoAcimaDoLimite = Anexo.TamanhoMaximoBytes + 1;

        Assert.Throws<DomainException>(() =>
            new Anexo(Guid.NewGuid(), "abc123.pdf", "nota.pdf", TipoArquivoAnexo.Pdf, tamanhoAcimaDoLimite));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Deve_rejeitar_tamanho_invalido(long tamanho)
    {
        Assert.Throws<DomainException>(() =>
            new Anexo(Guid.NewGuid(), "abc123.pdf", "nota.pdf", TipoArquivoAnexo.Pdf, tamanho));
    }
}
