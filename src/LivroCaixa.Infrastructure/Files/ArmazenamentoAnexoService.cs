using LivroCaixa.Application.Interfaces;
using LivroCaixa.Domain.Entities;
using LivroCaixa.Domain.Exceptions;

namespace LivroCaixa.Infrastructure.Files;

public class ArmazenamentoAnexoService : IArmazenamentoAnexoService
{
    private static readonly string[] ExtensoesPermitidas = { ".pdf", ".jpg", ".jpeg", ".png" };

    public async Task<(string CaminhoArmazenado, long TamanhoBytes)> SalvarAsync(Guid lancamentoId, string caminhoOrigem)
    {
        if (!File.Exists(caminhoOrigem))
            throw new DomainException("Arquivo não encontrado.");

        var extensao = Path.GetExtension(caminhoOrigem).ToLowerInvariant();
        if (!ExtensoesPermitidas.Contains(extensao))
            throw new DomainException("Tipo de arquivo não suportado. Use PDF, JPG ou PNG.");

        var tamanhoBytes = new FileInfo(caminhoOrigem).Length;
        if (tamanhoBytes > Anexo.TamanhoMaximoBytes)
            throw new DomainException(
                $"O arquivo excede o tamanho máximo permitido de {Anexo.TamanhoMaximoBytes / (1024 * 1024)}MB.");

        var pastaLancamento = Path.Combine(CaminhosAplicacao.DiretorioAnexos, lancamentoId.ToString());
        Directory.CreateDirectory(pastaLancamento);

        var nomeArmazenado = $"{Guid.NewGuid()}{extensao}";
        var caminhoDestino = Path.Combine(pastaLancamento, nomeArmazenado);

        await using (var origem = File.OpenRead(caminhoOrigem))
        await using (var destino = File.Create(caminhoDestino))
        {
            await origem.CopyToAsync(destino);
        }

        var caminhoRelativo = Path.Combine(lancamentoId.ToString(), nomeArmazenado);
        return (caminhoRelativo, tamanhoBytes);
    }

    public void Remover(string caminhoArmazenado)
    {
        var caminhoCompleto = ObterCaminhoCompleto(caminhoArmazenado);
        if (File.Exists(caminhoCompleto))
            File.Delete(caminhoCompleto);
    }

    public bool Existe(string caminhoArmazenado) => File.Exists(ObterCaminhoCompleto(caminhoArmazenado));

    public string ObterCaminhoCompleto(string caminhoArmazenado) =>
        Path.Combine(CaminhosAplicacao.DiretorioAnexos, caminhoArmazenado);
}
