namespace LivroCaixa.Application.DTOs;

public record RelatorioPeriodoDto(
    int Ano,
    int Mes,
    decimal TotalEntradas,
    decimal TotalSaidas,
    decimal Resultado,
    IReadOnlyList<ResultadoCategoriaDto> EntradasPorCategoria,
    IReadOnlyList<ResultadoCategoriaDto> SaidasPorCategoria,
    IReadOnlyList<LancamentoResumoDto> Extrato);
