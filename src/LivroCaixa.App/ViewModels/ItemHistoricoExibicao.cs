using LivroCaixa.Application.DTOs;
using LivroCaixa.Domain.Enums;

namespace LivroCaixa.App.ViewModels;

/// <summary>Um evento na linha do tempo do histórico da tela de detalhes do lançamento.</summary>
/// <param name="Explicacao">Frase curta sobre o evento (ex: "Removido do extrato"), mostrada quando não há campos alterados.</param>
/// <param name="EhUltimo">Último item da lista: não desenha a linha que liga ao próximo evento.</param>
public record ItemHistoricoExibicao(
    TipoEventoHistorico TipoEvento,
    string Titulo,
    DateTime DataHora,
    string Explicacao,
    IReadOnlyList<AlteracaoCampoDto> Alteracoes,
    bool EhUltimo)
{
    public bool TemAlteracoes => Alteracoes.Count > 0;
}
