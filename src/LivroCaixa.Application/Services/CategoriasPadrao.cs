using LivroCaixa.Domain.Entities;
using LivroCaixa.Domain.Enums;

namespace LivroCaixa.Application.Services;

/// <summary>
/// Conjunto padrão de categorias oferecido a toda empresa recém-cadastrada, para que o
/// cliente já comece a lançar sem precisar configurar nada (ver PLANO_DE_PROJETO.md, seção 1).
/// O conjunto varia conforme o segmento escolhido no cadastro — ver <see cref="Segmentos"/>
/// para as opções disponíveis na tela de cadastro inicial.
/// </summary>
public static class CategoriasPadrao
{
    public static class Segmentos
    {
        public const string AssociacaoOuSindicato = "Associação ou Sindicato";
        public const string Comercio = "Comércio";
        public const string Servicos = "Serviços";
        public const string Industria = "Indústria";
        public const string Outro = "Outro";

        // Associação/Sindicato primeiro na lista — é o perfil da maioria dos clientes reais do
        // escritório (ver PLANO_DE_PROJETO.md / memória do projeto).
        public static readonly string[] Todos = { AssociacaoOuSindicato, Comercio, Servicos, Industria, Outro };
    }

    private static readonly string[] NomesEntradaGenerico = { "Vendas", "Serviços", "Recebimentos", "Empréstimos", "Outros" };
    private static readonly string[] NomesSaidaGenerico = { "Fornecedores", "Salários", "Energia", "Internet", "Aluguel", "Impostos", "Combustível", "Material", "Outros" };

    private static readonly string[] NomesEntradaAssociacaoOuSindicato =
        { "Mensalidades de associados", "Contribuições sindicais", "Doações", "Convênios e parcerias", "Eventos e cursos", "Rendimentos financeiros", "Outros" };
    private static readonly string[] NomesSaidaAssociacaoOuSindicato =
        { "Salários e encargos", "Assessoria jurídica", "Assessoria contábil", "Aluguel", "Energia", "Água", "Internet e telefone", "Material de escritório", "Manutenção predial", "Eventos e assembleias", "Taxas bancárias", "Outros" };

    private static readonly string[] NomesEntradaComercio = { "Vendas", "Recebimentos", "Empréstimos", "Outros" };
    private static readonly string[] NomesSaidaComercio = { "Fornecedores", "Aluguel", "Energia", "Internet", "Salários", "Impostos", "Embalagens", "Frete", "Outros" };

    private static readonly string[] NomesEntradaServicos = { "Prestação de serviços", "Recebimentos", "Empréstimos", "Outros" };
    private static readonly string[] NomesSaidaServicos = { "Salários", "Aluguel", "Energia", "Internet", "Impostos", "Material de escritório", "Combustível", "Outros" };

    private static readonly string[] NomesEntradaIndustria = { "Vendas", "Recebimentos", "Empréstimos", "Outros" };
    private static readonly string[] NomesSaidaIndustria = { "Matéria-prima", "Fornecedores", "Salários", "Energia", "Aluguel", "Impostos", "Manutenção de equipamentos", "Frete", "Outros" };

    public static List<Categoria> Criar(Guid empresaId, string? segmento = null)
    {
        var (nomesEntrada, nomesSaida) = segmento switch
        {
            Segmentos.AssociacaoOuSindicato => (NomesEntradaAssociacaoOuSindicato, NomesSaidaAssociacaoOuSindicato),
            Segmentos.Comercio => (NomesEntradaComercio, NomesSaidaComercio),
            Segmentos.Servicos => (NomesEntradaServicos, NomesSaidaServicos),
            Segmentos.Industria => (NomesEntradaIndustria, NomesSaidaIndustria),
            _ => (NomesEntradaGenerico, NomesSaidaGenerico)
        };

        var categorias = new List<Categoria>();
        categorias.AddRange(nomesEntrada.Select(nome => new Categoria(empresaId, nome, TipoLancamento.Entrada)));
        categorias.AddRange(nomesSaida.Select(nome => new Categoria(empresaId, nome, TipoLancamento.Saida)));
        return categorias;
    }
}
