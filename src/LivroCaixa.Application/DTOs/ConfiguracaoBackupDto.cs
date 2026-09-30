using LivroCaixa.Domain.Enums;

namespace LivroCaixa.Application.DTOs;

public record ConfiguracaoBackupDto(
    bool Ativo,
    FrequenciaBackup Frequencia,
    string? PastaDestino,
    DateTime? UltimoBackupEm,
    DateTime? ProximoBackupEm);
