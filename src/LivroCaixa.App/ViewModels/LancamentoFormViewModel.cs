using System.Collections.ObjectModel;
using System.IO;
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

public partial class LancamentoFormViewModel : ObservableObject
{
    private readonly LancamentoService _lancamentoService;
    private readonly ICategoriaRepository _categoriaRepository;
    private readonly IContaRepository _contaRepository;
    private readonly AnexoService _anexoService;
    private readonly ModeloLancamentoService _modeloService;
    private readonly Guid _empresaId;
    private Guid? _lancamentoId;
    private List<Categoria> _todasCategorias = new();

    [ObservableProperty]
    private TipoLancamento tipo = TipoLancamento.Entrada;

    [ObservableProperty]
    private DateTime data = DateTime.Today;

    [ObservableProperty]
    private string valorTexto = string.Empty;

    [ObservableProperty]
    private Categoria? categoriaSelecionada;

    [ObservableProperty]
    private Conta? contaSelecionada;

    [ObservableProperty]
    private FormaPagamento formaPagamento = FormaPagamento.Dinheiro;

    [ObservableProperty]
    private string? descricao;

    [ObservableProperty]
    private string mensagemErro = string.Empty;

    [ObservableProperty]
    private bool estaSalvando;

    public bool EstaEditando => _lancamentoId is not null;

    public string TituloJanela => EstaEditando ? "Editar lançamento" : "Novo lançamento";

    public ObservableCollection<Categoria> CategoriasDisponiveis { get; } = new();

    public ObservableCollection<Conta> ContasDisponiveis { get; } = new();

    public ObservableCollection<AnexoResumoDto> Anexos { get; } = new();

    public ColecaoObservavel<ModeloLancamentoDto> ModelosDisponiveis { get; } = new();

    /// <summary>Só no lançamento novo: na edição o tipo é fixo e o modelo poderia trocar entrada por saída.</summary>
    public bool PodeUsarModelo => !EstaEditando && ModelosDisponiveis.Count > 0;

    [ObservableProperty]
    private ModeloLancamentoDto? modeloSelecionado;

    public event EventHandler? Concluido;

    public IAsyncRelayCommand SalvarCommand { get; }

    public IAsyncRelayCommand<Guid> RemoverAnexoCommand { get; }

    public LancamentoFormViewModel(
        LancamentoService lancamentoService,
        ICategoriaRepository categoriaRepository,
        IContaRepository contaRepository,
        AnexoService anexoService,
        ModeloLancamentoService modeloService,
        Guid empresaId,
        Guid? lancamentoIdEmEdicao)
    {
        _lancamentoService = lancamentoService;
        _categoriaRepository = categoriaRepository;
        _contaRepository = contaRepository;
        _anexoService = anexoService;
        _modeloService = modeloService;
        _empresaId = empresaId;
        _lancamentoId = lancamentoIdEmEdicao;

        SalvarCommand = new AsyncRelayCommand(SalvarAsync);
        RemoverAnexoCommand = new AsyncRelayCommand<Guid>(RemoverAnexoAsync);
    }

    public async Task InicializarAsync()
    {
        _todasCategorias = await _categoriaRepository.ListarPorEmpresaAsync(_empresaId);

        var contas = await _contaRepository.ListarPorEmpresaAsync(_empresaId);
        ContasDisponiveis.Clear();
        foreach (var conta in contas)
            ContasDisponiveis.Add(conta);

        if (_lancamentoId is Guid id)
        {
            var detalhe = await _lancamentoService.ObterDetalheAsync(id);
            Tipo = detalhe.Tipo;
            Data = detalhe.Data.ToDateTime(TimeOnly.MinValue);
            ValorTexto = ConversorValor.Formatar(detalhe.Valor);
            FormaPagamento = detalhe.FormaPagamento;
            Descricao = detalhe.Descricao;

            AtualizarCategoriasDisponiveis();
            CategoriaSelecionada = CategoriasDisponiveis.SingleOrDefault(c => c.Id == detalhe.CategoriaId);
            ContaSelecionada = ContasDisponiveis.SingleOrDefault(c => c.Id == detalhe.ContaId);
            await CarregarAnexosAsync();
        }
        else
        {
            AtualizarCategoriasDisponiveis();
            ContaSelecionada = ContasDisponiveis.FirstOrDefault();
            ModelosDisponiveis.SubstituirPor(await _modeloService.ListarAsync(_empresaId));
            OnPropertyChanged(nameof(PodeUsarModelo));
        }
    }

    /// <summary>
    /// Preenche o formulário com o modelo escolhido. Data não vem do modelo (é sempre a do dia, ou a
    /// que o usuário já escolheu); campos opcionais vazios no modelo mantêm o que já estava na tela.
    /// </summary>
    partial void OnModeloSelecionadoChanged(ModeloLancamentoDto? value)
    {
        if (value is null || EstaEditando)
            return;

        Tipo = value.Tipo;
        CategoriaSelecionada = CategoriasDisponiveis.FirstOrDefault(c => c.Id == value.CategoriaId) ?? CategoriaSelecionada;
        if (value.ContaId is Guid contaId)
            ContaSelecionada = ContasDisponiveis.FirstOrDefault(c => c.Id == contaId) ?? ContaSelecionada;
        FormaPagamento = value.FormaPagamento;
        if (value.Valor is decimal valor)
            ValorTexto = ConversorValor.Formatar(valor);
        Descricao = value.Descricao ?? value.Nome;
        MensagemErro = string.Empty;
    }

    /// <summary>
    /// Garante que o lançamento já exista no banco antes de anexar um comprovante — se o
    /// usuário ainda está criando um lançamento novo (nunca clicou em Salvar), ele é salvo
    /// silenciosamente agora, e a janela passa a se comportar como edição dali em diante.
    /// </summary>
    public async Task<bool> GarantirLancamentoCriadoAsync()
    {
        if (_lancamentoId is Guid idExistente)
            return true;

        if (!ValidarCampos(out var valor))
            return false;

        try
        {
            var dataSelecionada = DateOnly.FromDateTime(Data);
            var lancamento = await _lancamentoService.RegistrarAsync(_empresaId, new NovoLancamentoDto(
                ContaSelecionada!.Id, CategoriaSelecionada!.Id, Tipo, dataSelecionada, valor, FormaPagamento, Descricao, null));

            _lancamentoId = lancamento.Id;
            OnPropertyChanged(nameof(EstaEditando));
            OnPropertyChanged(nameof(PodeUsarModelo));
            OnPropertyChanged(nameof(TituloJanela));
            return true;
        }
        catch (DomainException ex)
        {
            MensagemErro = ex.Message;
            return false;
        }
    }

    private async Task CarregarAnexosAsync()
    {
        if (_lancamentoId is not Guid id)
            return;

        var lista = await _anexoService.ListarAsync(id);
        Anexos.Clear();
        foreach (var anexo in lista)
            Anexos.Add(anexo);
    }

    public Task AdicionarAnexoAsync(string caminhoArquivo) => AdicionarAnexosAsync(new[] { caminhoArquivo });

    /// <summary>
    /// Anexa vários arquivos de uma vez (arrastar e soltar). Um arquivo recusado (tipo/tamanho)
    /// não impede os outros; as mensagens de erro de todos os recusados ficam visíveis juntas.
    /// </summary>
    public async Task AdicionarAnexosAsync(IReadOnlyList<string> caminhosArquivos)
    {
        if (_lancamentoId is not Guid id)
            return;

        MensagemErro = string.Empty;
        var erros = new List<string>();
        foreach (var caminho in caminhosArquivos)
        {
            try
            {
                await _anexoService.AdicionarAsync(id, caminho);
            }
            catch (DomainException ex)
            {
                erros.Add(caminhosArquivos.Count > 1 ? $"{Path.GetFileName(caminho)}: {ex.Message}" : ex.Message);
            }
        }

        await CarregarAnexosAsync();
        MensagemErro = string.Join(Environment.NewLine, erros);
    }

    private async Task RemoverAnexoAsync(Guid anexoId)
    {
        if (_lancamentoId is not Guid id)
            return;

        MensagemErro = string.Empty;
        try
        {
            await _anexoService.RemoverAsync(id, anexoId);
            await CarregarAnexosAsync();
        }
        catch (DomainException ex)
        {
            MensagemErro = ex.Message;
        }
    }

    partial void OnTipoChanged(TipoLancamento value) => AtualizarCategoriasDisponiveis();

    private void AtualizarCategoriasDisponiveis()
    {
        CategoriasDisponiveis.Clear();
        foreach (var categoria in _todasCategorias.Where(c => c.Tipo == Tipo))
            CategoriasDisponiveis.Add(categoria);

        if (CategoriaSelecionada is null || CategoriaSelecionada.Tipo != Tipo)
            CategoriaSelecionada = CategoriasDisponiveis.FirstOrDefault();
    }

    private bool ValidarCampos(out decimal valor)
    {
        MensagemErro = string.Empty;

        if (!ConversorValor.TentarLer(ValorTexto, out valor))
        {
            MensagemErro = "Informe um valor numérico válido.";
            return false;
        }

        if (CategoriaSelecionada is null)
        {
            MensagemErro = "Selecione uma categoria.";
            return false;
        }

        if (ContaSelecionada is null)
        {
            MensagemErro = "Selecione uma conta.";
            return false;
        }

        return true;
    }

    private async Task SalvarAsync()
    {
        if (!ValidarCampos(out var valor))
            return;

        EstaSalvando = true;
        try
        {
            var dataSelecionada = DateOnly.FromDateTime(Data);

            if (_lancamentoId is Guid id)
            {
                await _lancamentoService.AtualizarAsync(new AtualizarLancamentoDto(
                    id, ContaSelecionada!.Id, CategoriaSelecionada!.Id, dataSelecionada, valor, FormaPagamento, Descricao, null));
            }
            else
            {
                var lancamento = await _lancamentoService.RegistrarAsync(_empresaId, new NovoLancamentoDto(
                    ContaSelecionada!.Id, CategoriaSelecionada!.Id, Tipo, dataSelecionada, valor, FormaPagamento, Descricao, null));
                _lancamentoId = lancamento.Id;
            }

            Concluido?.Invoke(this, EventArgs.Empty);
        }
        catch (DomainException ex)
        {
            MensagemErro = ex.Message;
        }
        finally
        {
            EstaSalvando = false;
        }
    }
}
