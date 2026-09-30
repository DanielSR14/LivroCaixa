using LivroCaixa.Domain.Exceptions;

namespace LivroCaixa.Domain.Entities;

/// <summary>
/// Dinheiro movido entre duas contas da própria organização (ex: saque do banco para o caixa).
/// Não é receita nem despesa: muda o saldo de cada conta, mas não entra nos totais de entradas/saídas,
/// nos gráficos nem no resultado por categoria — por isso é uma entidade à parte, e não um par de
/// Lancamento de entrada e saída (que inflaria entradas e saídas nos relatórios e no .contabil).
/// Não tem edição: para corrigir, exclui-se e registra-se de novo.
/// </summary>
public class Transferencia : EntidadeBase
{
    public Guid EmpresaId { get; private set; }
    public Guid ContaOrigemId { get; private set; }
    public Guid ContaDestinoId { get; private set; }
    public DateOnly Data { get; private set; }
    public decimal Valor { get; private set; }
    public string? Descricao { get; private set; }
    public DateTime CriadoEm { get; private set; }
    public DateTime? ExcluidoEm { get; private set; }

    private Transferencia()
    {
    }

    public Transferencia(Guid empresaId, Conta origem, Conta destino, DateOnly data, decimal valor, string? descricao = null)
    {
        if (empresaId == Guid.Empty)
            throw new DomainException("A transferência precisa estar vinculada a uma empresa.");
        if (origem is null || destino is null)
            throw new DomainException("Selecione a conta de origem e a de destino.");
        if (origem.EmpresaId != empresaId || destino.EmpresaId != empresaId)
            throw new DomainException("As contas selecionadas não pertencem a esta empresa.");
        if (origem.Id == destino.Id)
            throw new DomainException("A conta de origem e a de destino precisam ser diferentes.");
        if (valor <= 0)
            throw new DomainException("O valor da transferência deve ser maior que zero.");
        // Data local, não UTC — mesmo motivo de Lancamento.ValidarData.
        if (data > DateOnly.FromDateTime(DateTime.Today))
            throw new DomainException("A data da transferência não pode ser futura.");

        EmpresaId = empresaId;
        ContaOrigemId = origem.Id;
        ContaDestinoId = destino.Id;
        Data = data;
        Valor = valor;
        Descricao = string.IsNullOrWhiteSpace(descricao) ? null : descricao.Trim();
        CriadoEm = DateTime.UtcNow;
    }

    /// <summary>Exclusão lógica, com a mesma regra de competência fechada de <see cref="Lancamento.Excluir"/>.</summary>
    public void Excluir()
    {
        if (ExcluidoEm is not null)
            throw new DomainException("Esta transferência já está excluída.");

        var primeiroDiaDoMesAtual = new DateOnly(DateTime.Today.Year, DateTime.Today.Month, 1);
        if (new DateOnly(Data.Year, Data.Month, 1) < primeiroDiaDoMesAtual)
            throw new DomainException("Não é possível excluir transferências de meses anteriores ao atual. Se precisar corrigir algo já enviado, fale com o escritório de contabilidade.");

        ExcluidoEm = DateTime.UtcNow;
    }

    /// <summary>Efeito no saldo da conta informada: negativo na origem, positivo no destino, zero nas demais.</summary>
    public decimal EfeitoNoSaldo(Guid contaId) =>
        contaId == ContaOrigemId ? -Valor : contaId == ContaDestinoId ? Valor : 0m;
}
