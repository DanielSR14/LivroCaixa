namespace LivroCaixa.App;

/// <summary>
/// Bancos sugeridos ao criar uma conta, com o caminho do respectivo logo. Os arquivos ficam em
/// Assets/BancosLogos/Selecionados — cópias com nome simples (sem espaço, ponto ou acento) dos
/// arquivos originais em Assets/BancosLogos/&lt;Nome do banco&gt;/ (coleção de terceiros; ver o
/// README.md lá dentro para origem/licença de cada logo).
///
/// Importante: os nomes de pasta/arquivo originais (ex: "Banco do Brasil S.A", "Itaú Unibanco
/// S.A") têm ponto e acento, e isso quebra a resolução do caminho do recurso pelo WPF/SharpVectors
/// — só os bancos com nome "simples" (sem ponto nem acento, ex: Sicoob, Sicredi, Banrisul)
/// funcionavam antes desta cópia existir. Por isso: NUNCA referenciar os arquivos originais
/// diretamente no código — sempre passar por uma cópia em Selecionados/ com nome ascii simples.
///
/// Dois bancos da coleção original (Ifood Pago, Zemo Bank) não têm nenhuma variante colorida —
/// só logo branco — e foram deixados de fora daqui porque ficariam invisíveis no fundo claro do
/// app.
/// </summary>
public static class BancosConhecidos
{
    private const string Pasta = "/Assets/BancosLogos/Selecionados/";

    public static IReadOnlyList<ItemBancoSugerido> Lista { get; } = new List<ItemBancoSugerido>
    {
        new("ABC Brasil", Pasta + "abc-brasil.svg"),
        new("Ailos", Pasta + "ailos.svg"),
        new("Almah Conta", Pasta + "almah-conta.svg"),
        new("Artta", Pasta + "artta.svg"),
        new("Asaas", Pasta + "asaas.svg"),
        new("Banco Arbi", Pasta + "banco-arbi.svg"),
        new("Banco BMG", Pasta + "banco-bmg.svg"),
        new("Banco BMP", Pasta + "banco-bmp.svg"),
        new("Banco BS2", Pasta + "banco-bs2.svg"),
        new("Banco da Amazônia", Pasta + "banco-da-amazonia.svg"),
        new("Banco do Brasil", Pasta + "banco-do-brasil.svg"),
        new("Banco Industrial", Pasta + "banco-industrial.svg"),
        new("Banco Original", Pasta + "banco-original.svg"),
        new("Banco Pan", Pasta + "banco-pan.svg"),
        new("Banco Paulista", Pasta + "banco-paulista.svg"),
        new("Banco Pine", Pasta + "banco-pine.svg"),
        new("Banco Rendimento", Pasta + "banco-rendimento.svg"),
        new("Banco Topázio", Pasta + "banco-topazio.svg"),
        new("Banese", Pasta + "banese.svg"),
        new("Banestes", Pasta + "banestes.svg"),
        new("Bank of America", Pasta + "bank-of-america.svg"),
        new("Banpará", Pasta + "banpara.svg"),
        new("Banrisul", Pasta + "banrisul.svg"),
        new("Bees Bank", Pasta + "bees-bank.svg"),
        new("BK Bank", Pasta + "bk-bank.svg"),
        new("BNB", Pasta + "bnb.svg"),
        new("BNP Paribas", Pasta + "bnp-paribas.svg"),
        new("Bradesco", Pasta + "bradesco.svg"),
        new("BRB", Pasta + "brb.svg"),
        new("BTG Pactual", Pasta + "btg-pactual.svg"),
        new("C6 Bank", Pasta + "c6-bank.svg"),
        new("Caixa Econômica Federal", Pasta + "caixa-economica-federal.svg"),
        new("Capitual", Pasta + "capitual.svg"),
        new("Conta Simples", Pasta + "conta-simples.svg"),
        new("Contbank", Pasta + "contbank.svg"),
        new("Cora", Pasta + "cora.svg"),
        new("Credisis", Pasta + "credisis.svg"),
        new("Cresol", Pasta + "cresol.svg"),
        new("Daycoval", Pasta + "daycoval.svg"),
        new("Dock", Pasta + "dock.svg"),
        new("DuePay", Pasta + "duepay.svg"),
        new("Efí Bank", Pasta + "efi-bank.svg"),
        new("Grafeno", Pasta + "grafeno.svg"),
        new("InfinitePay", Pasta + "infinitepay.svg"),
        new("Inter", Pasta + "inter.svg"),
        new("Ip4y", Pasta + "ip4y.svg"),
        new("Itaú", Pasta + "itau.svg"),
        new("Iugu", Pasta + "iugu.svg"),
        new("Lets Bank", Pasta + "lets-bank.svg"),
        new("Linker", Pasta + "linker.svg"),
        new("MagaluPay", Pasta + "magalupay.svg"),
        new("Mercado Pago", Pasta + "mercado-pago.svg"),
        new("Mercantil do Brasil", Pasta + "mercantil-do-brasil.svg"),
        new("ModoBank", Pasta + "modobank.svg"),
        new("MUFG", Pasta + "mufg.svg"),
        new("Múltiplo Bank", Pasta + "multiplo-bank.svg"),
        new("Neon", Pasta + "neon.svg"),
        new("Nubank", Pasta + "nubank.svg"),
        new("Omie", Pasta + "omie.svg"),
        new("Omni", Pasta + "omni.svg"),
        new("OrionPay", Pasta + "orionpay.svg"),
        new("PagBank", Pasta + "pagbank.svg"),
        new("PayCash", Pasta + "paycash.svg"),
        new("PicPay", Pasta + "picpay.svg"),
        new("PinBank", Pasta + "pinbank.svg"),
        new("Quality Digital Bank", Pasta + "quality-digital-bank.svg"),
        new("RecargaPay", Pasta + "recargapay.svg"),
        new("Safra", Pasta + "safra.svg"),
        new("Santander", Pasta + "santander.svg"),
        new("Sicoob", Pasta + "sicoob.svg"),
        new("Sicredi", Pasta + "sicredi.svg"),
        new("Sisprime", Pasta + "sisprime.svg"),
        new("Sofisa", Pasta + "sofisa.svg"),
        new("Squid", Pasta + "squid.svg"),
        new("StarBank", Pasta + "starbank.svg"),
        new("Stone", Pasta + "stone.svg"),
        new("Sulcredi", Pasta + "sulcredi.svg"),
        new("Transfeera", Pasta + "transfeera.svg"),
        new("Tribanco", Pasta + "tribanco.svg"),
        new("Unicred", Pasta + "unicred.svg"),
        new("Uniprime", Pasta + "uniprime.svg"),
        new("UzziPay", Pasta + "uzzipay.svg"),
        new("Votorantim", Pasta + "votorantim.svg"),
        new("XP Investimentos", Pasta + "xp-investimentos.svg"),
    }.OrderBy(b => b.Nome).ToList();

    /// <summary>
    /// Retorna o caminho do logo do banco cujo nome bate (sem diferenciar maiúsculas/minúsculas)
    /// com o nome da conta informado, ou null se não houver correspondência.
    /// </summary>
    public static string? ObterIcone(string? nomeConta)
    {
        if (string.IsNullOrWhiteSpace(nomeConta))
            return null;

        var nomeNormalizado = nomeConta.Trim();
        return Lista.FirstOrDefault(b => string.Equals(b.Nome, nomeNormalizado, StringComparison.OrdinalIgnoreCase))?.IconeCaminho;
    }
}
