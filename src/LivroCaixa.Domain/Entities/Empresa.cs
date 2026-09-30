using LivroCaixa.Domain.Exceptions;

namespace LivroCaixa.Domain.Entities;

public class Empresa : EntidadeBase
{
    public string RazaoSocial { get; private set; } = null!;
    public string Cnpj { get; private set; } = null!;
    public string? NomeFantasia { get; private set; }
    public string? Segmento { get; private set; }
    public DateTime DataCadastro { get; private set; }

    /// <summary>Opções de bloqueio por inatividade oferecidas nas Configurações. 0 = nunca bloquear.</summary>
    public static IReadOnlyList<int> OpcoesBloqueioInatividadeMinutos { get; } = new[] { 0, 5, 10, 15, 30, 60 };

    public const int BloqueioInatividadePadraoMinutos = 10;

    public int MinutosBloqueioInatividade { get; private set; } = BloqueioInatividadePadraoMinutos;

    /// <summary>Versão do termo de uso/privacidade aceita (null = nunca aceitou; versão nova exige novo aceite).</summary>
    public string? VersaoTermoAceita { get; private set; }

    public DateTime? TermoAceitoEm { get; private set; }

    /// <summary>
    /// Nome usado em telas e saudações: o nome fantasia (mais reconhecível para o próprio
    /// dono do negócio) quando existir, senão a razão social.
    /// </summary>
    public string NomeExibicao => string.IsNullOrWhiteSpace(NomeFantasia) ? RazaoSocial : NomeFantasia!;

    private Empresa()
    {
    }

    public Empresa(string razaoSocial, string cnpj, string? segmento = null, string? nomeFantasia = null)
    {
        if (string.IsNullOrWhiteSpace(razaoSocial))
            throw new DomainException("A razão social da empresa é obrigatória.");

        ValidarCnpj(cnpj);

        RazaoSocial = razaoSocial.Trim();
        Cnpj = cnpj.Trim();
        NomeFantasia = NormalizarTexto(nomeFantasia);
        Segmento = NormalizarTexto(segmento);
        DataCadastro = DateTime.UtcNow;
    }

    public void AtualizarSegmento(string? segmento)
    {
        Segmento = NormalizarTexto(segmento);
    }

    public void DefinirBloqueioInatividade(int minutos)
    {
        if (!OpcoesBloqueioInatividadeMinutos.Contains(minutos))
            throw new DomainException("Tempo de bloqueio por inatividade inválido.");

        MinutosBloqueioInatividade = minutos;
    }

    public void AceitarTermo(string versao)
    {
        if (string.IsNullOrWhiteSpace(versao))
            throw new DomainException("A versão do termo de uso é obrigatória.");

        VersaoTermoAceita = versao.Trim();
        TermoAceitoEm = DateTime.UtcNow;
    }

    public bool AceitouTermo(string versaoAtual) => VersaoTermoAceita == versaoAtual;

    private static void ValidarCnpj(string cnpj)
    {
        if (string.IsNullOrWhiteSpace(cnpj))
            throw new DomainException("O CNPJ da empresa é obrigatório.");

        var digitos = new string(cnpj.Where(char.IsDigit).ToArray());
        if (digitos.Length != 14)
            throw new DomainException("O CNPJ deve conter 14 dígitos.");
    }

    private static string? NormalizarTexto(string? texto) =>
        string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();
}
