using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LivroCaixa.Application;
using LivroCaixa.Application.DTOs;
using LivroCaixa.Application.Interfaces;
using LivroCaixa.Application.Services;
using LivroCaixa.Domain.Entities;
using LivroCaixa.Domain.Enums;
using LivroCaixa.Domain.Exceptions;

namespace LivroCaixa.App.ViewModels;

/// <summary>Formulário de criar/editar modelo de lançamento (mesmo padrão duplo-uso de NovaCategoria/NovaConta).</summary>
public partial class ModeloFormViewModel : ObservableObject
{
    private static readonly ItemContaFiltro SemContaPadrao = new(null, "Nenhuma (usar a conta já escolhida no lançamento)");

    private readonly ModeloLancamentoService _modeloService;
    private readonly ICategoriaRepository _categoriaRepository;
    private readonly IContaRepository _contaRepository;
    private readonly Guid _empresaId;
    private readonly ModeloLancamentoDto? _modeloEmEdicao;

    [ObservableProperty]
    private string codigo = string.Empty;

    [ObservableProperty]
    private string nome = string.Empty;

    [ObservableProperty]
    private Categoria? categoriaSelecionada;

    [ObservableProperty]
    private ItemContaFiltro contaSelecionada = SemContaPadrao;

    [ObservableProperty]
    private FormaPagamento formaPagamento = FormaPagamento.Pix;

    [ObservableProperty]
    private string valorTexto = string.Empty;

    [ObservableProperty]
    private string? descricao;

    [ObservableProperty]
    private string mensagemErro = string.Empty;

    public bool EstaEditando => _modeloEmEdicao is not null;

    public string TituloJanela => EstaEditando ? "Editar modelo" : "Novo modelo de lançamento";

    public int TamanhoMaximoCodigo => ModeloLancamento.TamanhoMaximoCodigo;

    public ColecaoObservavel<Categoria> Categorias { get; } = new();

    public ColecaoObservavel<ItemContaFiltro> Contas { get; } = new();

    public event EventHandler? Concluido;

    public IAsyncRelayCommand SalvarCommand { get; }

    public ModeloFormViewModel(
        ModeloLancamentoService modeloService,
        ICategoriaRepository categoriaRepository,
        IContaRepository contaRepository,
        Guid empresaId,
        ModeloLancamentoDto? modeloEmEdicao)
    {
        _modeloService = modeloService;
        _categoriaRepository = categoriaRepository;
        _contaRepository = contaRepository;
        _empresaId = empresaId;
        _modeloEmEdicao = modeloEmEdicao;
        SalvarCommand = new AsyncRelayCommand(SalvarAsync);
    }

    public async Task InicializarAsync()
    {
        var categorias = await _categoriaRepository.ListarPorEmpresaAsync(_empresaId);
        Categorias.SubstituirPor(categorias.OrderBy(c => c.Tipo).ThenBy(c => c.Nome));

        var contas = await _contaRepository.ListarPorEmpresaAsync(_empresaId);
        Contas.SubstituirPor(new[] { SemContaPadrao }.Concat(contas.OrderBy(c => c.Nome).Select(c => new ItemContaFiltro(c.Id, c.Nome))));

        if (_modeloEmEdicao is { } modelo)
        {
            Codigo = modelo.Codigo;
            Nome = modelo.Nome;
            CategoriaSelecionada = Categorias.FirstOrDefault(c => c.Id == modelo.CategoriaId);
            ContaSelecionada = Contas.FirstOrDefault(c => c.Id == modelo.ContaId) ?? SemContaPadrao;
            FormaPagamento = modelo.FormaPagamento;
            ValorTexto = modelo.Valor is decimal valor ? ConversorValor.Formatar(valor) : string.Empty;
            Descricao = modelo.Descricao;
        }
        else
        {
            CategoriaSelecionada = Categorias.FirstOrDefault(c => c.Tipo == TipoLancamento.Saida) ?? Categorias.FirstOrDefault();
        }
    }

    private async Task SalvarAsync()
    {
        MensagemErro = string.Empty;

        if (CategoriaSelecionada is null)
        {
            MensagemErro = "Selecione uma categoria.";
            return;
        }

        if (!ConversorValor.TentarLerOpcional(ValorTexto, out var valor))
        {
            MensagemErro = "Informe um valor numérico válido, ou deixe em branco.";
            return;
        }

        try
        {
            var dto = new SalvarModeloLancamentoDto(
                Codigo, Nome, CategoriaSelecionada.Id, ContaSelecionada.Id, FormaPagamento, valor, Descricao);

            if (_modeloEmEdicao is { } modelo)
                await _modeloService.AtualizarAsync(modelo.Id, dto);
            else
                await _modeloService.CriarAsync(_empresaId, dto);

            Concluido?.Invoke(this, EventArgs.Empty);
        }
        catch (DomainException ex)
        {
            MensagemErro = ex.Message;
        }
    }
}

public class ModeloFormViewModelFactory
{
    private readonly ModeloLancamentoService _modeloService;
    private readonly ICategoriaRepository _categoriaRepository;
    private readonly IContaRepository _contaRepository;

    public ModeloFormViewModelFactory(
        ModeloLancamentoService modeloService, ICategoriaRepository categoriaRepository, IContaRepository contaRepository)
    {
        _modeloService = modeloService;
        _categoriaRepository = categoriaRepository;
        _contaRepository = contaRepository;
    }

    public ModeloFormViewModel CriarParaNovo(Guid empresaId) =>
        new(_modeloService, _categoriaRepository, _contaRepository, empresaId, null);

    public ModeloFormViewModel CriarParaEdicao(Guid empresaId, ModeloLancamentoDto modelo) =>
        new(_modeloService, _categoriaRepository, _contaRepository, empresaId, modelo);
}
