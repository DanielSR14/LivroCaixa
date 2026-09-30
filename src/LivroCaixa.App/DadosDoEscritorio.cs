namespace LivroCaixa.App;

/// <summary>
/// Dados de contato do escritório de contabilidade que distribui o app — ponto único de
/// configuração usado na tela de login, na tela "Sobre", no termo de uso e na mensagem de
/// e-mail pós-exportação (ver IDEIAS_E_MELHORIAS.md, itens 2 e 4).
/// Os valores abaixo são genéricos: cada escritório preenche com os próprios dados antes de
/// gerar o instalador. Campos nulos simplesmente não aparecem nas telas.
/// </summary>
public static class DadosDoEscritorio
{
    public const string Nome = "Seu Escritório de Contabilidade";
    public const string RazaoSocial = "Seu Escritório de Contabilidade";
    public const string EmailContabil = "contato@example.com";
    public const string PropostaDeValor = "Contabilidade que cuida do seu negócio para você focar no que importa.";

    public static string? Cnpj => null;
    public static string? Endereco => null;
    public static string? Telefone => null;
}
