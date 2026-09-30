using LivroCaixa.Domain.Enums;
using LivroCaixa.Domain.Exceptions;

namespace LivroCaixa.Domain.Entities;

public class Lancamento : EntidadeBase
{
    private readonly List<Anexo> _anexos = new();

    public Guid EmpresaId { get; private set; }
    public Guid ContaId { get; private set; }
    public Guid CategoriaId { get; private set; }
    public TipoLancamento Tipo { get; private set; }
    public DateOnly Data { get; private set; }
    public decimal Valor { get; private set; }
    public FormaPagamento FormaPagamento { get; private set; }
    public string? Descricao { get; private set; }
    public string? Observacoes { get; private set; }
    public DateTime CriadoEm { get; private set; }
    public DateTime? AlteradoEm { get; private set; }
    public DateTime? ExcluidoEm { get; private set; }
    public IReadOnlyCollection<Anexo> Anexos => _anexos.AsReadOnly();

    private Lancamento()
    {
    }

    public Lancamento(
        Guid empresaId,
        Conta conta,
        Categoria categoria,
        TipoLancamento tipo,
        DateOnly data,
        decimal valor,
        FormaPagamento formaPagamento,
        string? descricao = null,
        string? observacoes = null)
    {
        ValidarVinculos(empresaId, conta, categoria, tipo);
        ValidarValor(valor);
        ValidarData(data);

        EmpresaId = empresaId;
        ContaId = conta.Id;
        CategoriaId = categoria.Id;
        Tipo = tipo;
        Data = data;
        Valor = valor;
        FormaPagamento = formaPagamento;
        Descricao = NormalizarTexto(descricao);
        Observacoes = NormalizarTexto(observacoes);
        CriadoEm = DateTime.UtcNow;
    }

    public void Atualizar(
        Conta conta,
        Categoria categoria,
        DateOnly data,
        decimal valor,
        FormaPagamento formaPagamento,
        string? descricao,
        string? observacoes)
    {
        ValidarVinculos(EmpresaId, conta, categoria, Tipo);
        ValidarValor(valor);
        ValidarData(data);

        ContaId = conta.Id;
        CategoriaId = categoria.Id;
        Data = data;
        Valor = valor;
        FormaPagamento = formaPagamento;
        Descricao = NormalizarTexto(descricao);
        Observacoes = NormalizarTexto(observacoes);
        AlteradoEm = DateTime.UtcNow;
    }

    public void Excluir()
    {
        if (ExcluidoEm is not null)
            throw new DomainException("Este lançamento já está excluído.");

        // Uma vez que o mês vira, a competência é considerada fechada para o cliente — evita que um
        // lançamento já eventualmente exportado ao escritório suma retroativamente do extrato ativo
        // sem deixar rastro nenhum na competência em que ele foi enviado.
        var primeiroDiaDoMesAtual = new DateOnly(DateTime.Today.Year, DateTime.Today.Month, 1);
        var primeiroDiaDoMesDoLancamento = new DateOnly(Data.Year, Data.Month, 1);
        if (primeiroDiaDoMesDoLancamento < primeiroDiaDoMesAtual)
            throw new DomainException("Não é possível excluir lançamentos de meses anteriores ao atual. Se precisar corrigir algo já enviado, fale com o escritório de contabilidade.");

        ExcluidoEm = DateTime.UtcNow;
    }

    public void Restaurar()
    {
        if (ExcluidoEm is null)
            throw new DomainException("Este lançamento não está excluído.");

        ExcluidoEm = null;
    }

    public void AdicionarAnexo(Anexo anexo)
    {
        if (anexo.LancamentoId != Id)
            throw new DomainException("O anexo não pertence a este lançamento.");

        _anexos.Add(anexo);
    }

    public void RemoverAnexo(Guid anexoId)
    {
        _anexos.RemoveAll(a => a.Id == anexoId);
    }

    private static void ValidarVinculos(Guid empresaId, Conta conta, Categoria categoria, TipoLancamento tipo)
    {
        if (empresaId == Guid.Empty)
            throw new DomainException("O lançamento precisa estar vinculado a uma empresa.");

        if (conta is null)
            throw new DomainException("Selecione uma conta para o lançamento.");

        if (categoria is null)
            throw new DomainException("Selecione uma categoria para o lançamento.");

        if (conta.EmpresaId != empresaId)
            throw new DomainException("A conta selecionada não pertence a esta empresa.");

        if (categoria.EmpresaId != empresaId)
            throw new DomainException("A categoria selecionada não pertence a esta empresa.");

        if (categoria.Tipo != tipo)
            throw new DomainException("O tipo da categoria não corresponde ao tipo do lançamento (entrada/saída).");
    }

    private static void ValidarValor(decimal valor)
    {
        if (valor <= 0)
            throw new DomainException("O valor do lançamento deve ser maior que zero.");
    }

    private static void ValidarData(DateOnly data)
    {
        // Data local (a mesma que o usuário escolhe na tela), não UTC: no Brasil, entre 21h e
        // meia-noite o UTC já está no dia seguinte e aceitaria um lançamento com a data de amanhã.
        if (data > DateOnly.FromDateTime(DateTime.Today))
            throw new DomainException("A data do lançamento não pode ser futura.");
    }

    private static string? NormalizarTexto(string? texto) =>
        string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();
}
