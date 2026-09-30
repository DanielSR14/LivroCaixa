using LivroCaixa.Domain.Enums;
using LivroCaixa.Domain.Exceptions;

namespace LivroCaixa.Domain.Entities;

/// <summary>
/// Preferência de backup automático agendado da empresa. Existe no máximo uma por empresa
/// (criada sob demanda, desativada por padrão, na primeira vez que é consultada).
/// </summary>
public class ConfiguracaoBackup : EntidadeBase
{
    public Guid EmpresaId { get; private set; }
    public bool Ativo { get; private set; }
    public FrequenciaBackup Frequencia { get; private set; }
    public string? PastaDestino { get; private set; }
    public DateTime? UltimoBackupEm { get; private set; }

    private ConfiguracaoBackup()
    {
    }

    public ConfiguracaoBackup(Guid empresaId)
    {
        if (empresaId == Guid.Empty)
            throw new DomainException("A configuração de backup precisa estar vinculada a uma empresa.");

        EmpresaId = empresaId;
        Ativo = false;
        Frequencia = FrequenciaBackup.Semanal;
    }

    public void Configurar(bool ativo, FrequenciaBackup frequencia, string? pastaDestino)
    {
        var pastaNormalizada = string.IsNullOrWhiteSpace(pastaDestino) ? null : pastaDestino.Trim();

        if (ativo && pastaNormalizada is null)
            throw new DomainException("Escolha uma pasta de destino para ativar o backup automático.");

        Ativo = ativo;
        Frequencia = frequencia;
        PastaDestino = pastaNormalizada;
    }

    public void RegistrarBackupRealizado(DateTime momento) => UltimoBackupEm = momento;

    /// <summary>
    /// Quando o próximo backup automático deve acontecer, ou null se o backup automático está
    /// desativado. <see cref="DateTime.MinValue"/> significa "assim que possível" (nunca rodou
    /// um backup ainda, então já está vencido independente da frequência escolhida).
    /// </summary>
    public DateTime? ObterProximoBackupEm()
    {
        if (!Ativo || PastaDestino is null)
            return null;

        if (UltimoBackupEm is null)
            return DateTime.MinValue;

        return Frequencia switch
        {
            FrequenciaBackup.Diaria => UltimoBackupEm.Value.AddDays(1),
            FrequenciaBackup.Semanal => UltimoBackupEm.Value.AddDays(7),
            FrequenciaBackup.Mensal => UltimoBackupEm.Value.AddMonths(1),
            _ => null
        };
    }

    /// <summary>Se, na data/hora informada, já passou tempo suficiente desde o último backup segundo a frequência configurada.</summary>
    public bool PrecisaFazerBackupAgora(DateTime agora)
    {
        var proximoBackupEm = ObterProximoBackupEm();
        return proximoBackupEm is not null && agora >= proximoBackupEm;
    }
}
