using LivroCaixa.Domain.Enums;

namespace LivroCaixa.Application.DTOs;

public record ModeloLancamentoDto(
    Guid Id,
    string Codigo,
    string Nome,
    TipoLancamento Tipo,
    Guid CategoriaId,
    string NomeCategoria,
    Guid? ContaId,
    string? NomeConta,
    FormaPagamento FormaPagamento,
    decimal? Valor,
    string? Descricao)
{
    /// <summary>Texto da lista de sugestões do formulário: digitar o código já encontra o modelo.</summary>
    public string Exibicao => $"{Codigo} — {Nome}";
}

public record SalvarModeloLancamentoDto(
    string Codigo,
    string Nome,
    Guid CategoriaId,
    Guid? ContaId,
    FormaPagamento FormaPagamento,
    decimal? Valor,
    string? Descricao);
