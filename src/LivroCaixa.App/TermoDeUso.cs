namespace LivroCaixa.App;

/// <summary>
/// Termo de uso e privacidade (LGPD) aceito na primeira execução. Ao mudar o conteúdo de forma
/// relevante, incrementar <see cref="VersaoAtual"/>: quem aceitou a versão anterior vê o termo
/// de novo no próximo login (ver SegurancaService.PrecisaAceitarTermoAsync).
/// Texto escrito em linguagem simples; convém uma revisão jurídica pelo escritório.
/// </summary>
public static class TermoDeUso
{
    public const string VersaoAtual = "1";

    public const string Titulo = "Termo de uso e privacidade";

    // Record (e não tupla) porque o binding do WPF só enxerga propriedades, e os Item1/Item2 de uma tupla são campos.
    public record Secao(string Titulo, string Texto);

    public static IReadOnlyList<Secao> Secoes { get; } = new List<Secao>
    {
        new("O que é este aplicativo",
            $"O Livro Caixa é um aplicativo gratuito fornecido pelo escritório {DadosDoEscritorio.RazaoSocial}" +
            (DadosDoEscritorio.Cnpj is null ? "" : $" (CNPJ {DadosDoEscritorio.Cnpj})") +
            " aos seus clientes, para registrar as entradas e saídas financeiras " +
            "da organização, guardar os comprovantes e enviar essas informações ao escritório de contabilidade."),

        new("Onde seus dados ficam",
            "Todos os dados (cadastro da organização, usuário, contas, lançamentos e comprovantes anexados) ficam " +
            "gravados somente neste computador. A senha é guardada de forma protegida (criptografada), nunca em texto. " +
            "O aplicativo não envia nada pela internet e não tem acesso remoto aos seus dados."),

        new("O que é enviado ao escritório",
            "Nada é enviado automaticamente. Quando você usa a opção \"Exportar\", o aplicativo gera um arquivo " +
            "(.contabil) com os lançamentos, comprovantes e histórico de alterações da competência escolhida, e é você " +
            "quem decide enviá-lo ao escritório. O escritório usa essas informações exclusivamente para prestar os " +
            "serviços contábeis contratados e cumprir obrigações legais e fiscais."),

        new("Cópias de segurança",
            "Os backups (automáticos ou manuais) são gravados apenas na pasta que você escolher. Guarde-os em um local " +
            "seguro: quem tiver acesso a um backup tem acesso aos dados nele contidos."),

        new("Sua responsabilidade",
            "Mantenha sua senha e seu código de recuperação em segredo e o computador protegido. Os dados informados " +
            "são de responsabilidade de quem os registra; confira os lançamentos antes de exportar."),

        new("Registro técnico de erros",
            "Para ajudar a resolver problemas, o aplicativo guarda neste computador um registro técnico de erros. " +
            "Esse registro não contém valores, descrições de lançamentos nem documentos, e só chega ao escritório se " +
            "você mesmo o enviar."),

        new("Seus direitos (LGPD)",
            "Conforme a Lei Geral de Proteção de Dados (Lei nº 13.709/2018), você pode solicitar ao escritório " +
            "informações sobre o tratamento dos dados que enviou, sua correção ou eliminação, quando cabível. " +
            $"Contato: {DadosDoEscritorio.EmailContabil}" +
            (DadosDoEscritorio.Telefone is null ? "." : $" · {DadosDoEscritorio.Telefone}."))
    };
}
