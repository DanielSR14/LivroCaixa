namespace LivroCaixa.Application.DTOs;

public record ResumoCompetenciaDto(
    int Ano,
    int Mes,
    int QuantidadeLancamentos,
    int QuantidadeAnexos,
    long TamanhoTotalBytes,
    bool JaExportada);
