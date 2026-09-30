using LivroCaixa.Domain.Enums;
using LivroCaixa.Domain.Exceptions;

namespace LivroCaixa.Domain.Entities;

public class Anexo : EntidadeBase
{
    public const long TamanhoMaximoBytes = 20 * 1024 * 1024;

    public Guid LancamentoId { get; private set; }
    public string CaminhoArquivo { get; private set; } = null!;
    public string NomeOriginal { get; private set; } = null!;
    public TipoArquivoAnexo TipoArquivo { get; private set; }
    public long TamanhoBytes { get; private set; }
    public DateTime CriadoEm { get; private set; }

    private Anexo()
    {
    }

    public Anexo(Guid lancamentoId, string caminhoArquivo, string nomeOriginal, TipoArquivoAnexo tipoArquivo, long tamanhoBytes)
    {
        if (lancamentoId == Guid.Empty)
            throw new DomainException("O anexo precisa estar vinculado a um lançamento.");

        if (string.IsNullOrWhiteSpace(caminhoArquivo))
            throw new DomainException("O caminho do arquivo anexado é obrigatório.");

        if (string.IsNullOrWhiteSpace(nomeOriginal))
            throw new DomainException("O nome original do arquivo anexado é obrigatório.");

        if (tamanhoBytes <= 0)
            throw new DomainException("O tamanho do arquivo anexado é inválido.");

        if (tamanhoBytes > TamanhoMaximoBytes)
            throw new DomainException(
                $"O arquivo anexado excede o tamanho máximo permitido de {TamanhoMaximoBytes / (1024 * 1024)}MB.");

        LancamentoId = lancamentoId;
        CaminhoArquivo = caminhoArquivo.Trim();
        NomeOriginal = nomeOriginal.Trim();
        TipoArquivo = tipoArquivo;
        TamanhoBytes = tamanhoBytes;
        CriadoEm = DateTime.UtcNow;
    }
}
