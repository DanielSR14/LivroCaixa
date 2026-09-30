using LivroCaixa.Domain.Entities;
using LivroCaixa.Domain.Enums;
using LivroCaixa.Domain.Exceptions;

namespace LivroCaixa.Tests.Domain;

/// <summary>Agenda do backup automático, incluindo os limites de calendário (meses de tamanhos diferentes).</summary>
public class ConfiguracaoBackupTests
{
    private static ConfiguracaoBackup Ativa(FrequenciaBackup frequencia, DateTime? ultimoBackup)
    {
        var configuracao = new ConfiguracaoBackup(Guid.NewGuid());
        configuracao.Configurar(true, frequencia, @"C:\Backups");
        if (ultimoBackup is DateTime momento)
            configuracao.RegistrarBackupRealizado(momento);
        return configuracao;
    }

    [Fact]
    public void Nova_configuracao_nasce_desativada_e_nunca_pede_backup()
    {
        var configuracao = new ConfiguracaoBackup(Guid.NewGuid());

        Assert.False(configuracao.Ativo);
        Assert.Null(configuracao.ObterProximoBackupEm());
        Assert.False(configuracao.PrecisaFazerBackupAgora(DateTime.MaxValue));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Ativar_sem_pasta_de_destino_deve_ser_recusado(string? pasta)
    {
        var configuracao = new ConfiguracaoBackup(Guid.NewGuid());

        Assert.Throws<DomainException>(() => configuracao.Configurar(true, FrequenciaBackup.Diaria, pasta));
    }

    [Fact]
    public void Pasta_com_espacos_nas_pontas_deve_ser_normalizada()
    {
        var configuracao = new ConfiguracaoBackup(Guid.NewGuid());

        configuracao.Configurar(true, FrequenciaBackup.Diaria, @"  C:\Backups  ");

        Assert.Equal(@"C:\Backups", configuracao.PastaDestino);
    }

    [Fact]
    public void Ativo_e_nunca_executado_deve_pedir_backup_imediatamente()
    {
        Assert.True(Ativa(FrequenciaBackup.Mensal, null).PrecisaFazerBackupAgora(new DateTime(2026, 1, 1)));
    }

    [Theory]
    [InlineData(FrequenciaBackup.Diaria, 1)]
    [InlineData(FrequenciaBackup.Semanal, 7)]
    public void Deve_pedir_backup_exatamente_quando_o_prazo_vence(FrequenciaBackup frequencia, int dias)
    {
        var ultimo = new DateTime(2026, 3, 10, 14, 30, 0);
        var configuracao = Ativa(frequencia, ultimo);

        Assert.False(configuracao.PrecisaFazerBackupAgora(ultimo.AddDays(dias).AddMinutes(-1)));
        Assert.True(configuracao.PrecisaFazerBackupAgora(ultimo.AddDays(dias)));
    }

    // 31/01 + 1 mês não existe: o calendário "encurta" para o último dia de fevereiro, e o backup
    // não pode ficar esperando até março.
    [Theory]
    [InlineData(2026, 28)]
    [InlineData(2024, 29)]
    public void Mensal_a_partir_do_dia_31_deve_vencer_no_ultimo_dia_de_fevereiro(int ano, int ultimoDiaFevereiro)
    {
        var configuracao = Ativa(FrequenciaBackup.Mensal, new DateTime(ano, 1, 31, 9, 0, 0));

        Assert.Equal(new DateTime(ano, 2, ultimoDiaFevereiro, 9, 0, 0), configuracao.ObterProximoBackupEm());
    }

    [Fact]
    public void Desativar_deve_parar_de_pedir_backup_mesmo_com_prazo_vencido()
    {
        var configuracao = Ativa(FrequenciaBackup.Diaria, new DateTime(2020, 1, 1));

        configuracao.Configurar(false, FrequenciaBackup.Diaria, null);

        Assert.False(configuracao.PrecisaFazerBackupAgora(DateTime.Now));
    }
}
