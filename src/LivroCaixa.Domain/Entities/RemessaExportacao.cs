using LivroCaixa.Domain.Exceptions;

namespace LivroCaixa.Domain.Entities;

/// <summary>
/// Registro histórico de que uma competência foi exportada para o escritório. Serve de
/// base segura para a limpeza de comprovantes: só é permitido apagar o arquivo físico de
/// um comprovante de uma competência que já tem uma remessa registrada aqui, porque nesse
/// caso o arquivo já está salvo em outro lugar (o .contabil enviado).
/// </summary>
public class RemessaExportacao : EntidadeBase
{
    public Guid EmpresaId { get; private set; }
    public int Ano { get; private set; }
    public int Mes { get; private set; }
    public DateTime GeradoEm { get; private set; }
    public int QuantidadeLancamentos { get; private set; }
    public string CaminhoArquivoGerado { get; private set; } = null!;

    private RemessaExportacao()
    {
    }

    public RemessaExportacao(Guid empresaId, int ano, int mes, int quantidadeLancamentos, string caminhoArquivoGerado)
    {
        if (empresaId == Guid.Empty)
            throw new DomainException("A remessa de exportação precisa estar vinculada a uma empresa.");

        if (mes < 1 || mes > 12)
            throw new DomainException("Mês inválido para a remessa de exportação.");

        if (ano < 2000 || ano > 2100)
            throw new DomainException("Ano inválido para a remessa de exportação.");

        if (quantidadeLancamentos <= 0)
            throw new DomainException("A remessa de exportação precisa conter pelo menos um lançamento.");

        if (string.IsNullOrWhiteSpace(caminhoArquivoGerado))
            throw new DomainException("O caminho do arquivo gerado é obrigatório.");

        EmpresaId = empresaId;
        Ano = ano;
        Mes = mes;
        QuantidadeLancamentos = quantidadeLancamentos;
        CaminhoArquivoGerado = caminhoArquivoGerado.Trim();
        GeradoEm = DateTime.UtcNow;
    }
}
