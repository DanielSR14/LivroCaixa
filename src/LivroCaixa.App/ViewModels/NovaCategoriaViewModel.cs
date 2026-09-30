using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LivroCaixa.Application.Services;
using LivroCaixa.Domain.Entities;
using LivroCaixa.Domain.Enums;
using LivroCaixa.Domain.Exceptions;

namespace LivroCaixa.App.ViewModels;

public partial class NovaCategoriaViewModel : ObservableObject
{
    private readonly CategoriaService _categoriaService;
    private readonly Guid _empresaId;
    private readonly Guid? _categoriaIdEmEdicao;

    public bool EstaEditando => _categoriaIdEmEdicao is not null;

    public string TituloJanela => EstaEditando ? "Editar categoria" : "Nova categoria";

    public string TextoBotaoSalvar => EstaEditando ? "Salvar alterações" : "Criar categoria";

    /// <summary>O tipo (entrada/saída) é fixo desde a criação — não pode ser alterado ao editar
    /// (mesma regra de Lancamento.Tipo, para nunca deixar lançamentos existentes inconsistentes).</summary>
    public bool PodeEditarTipo => !EstaEditando;

    [ObservableProperty]
    private string nome = string.Empty;

    [ObservableProperty]
    private TipoLancamento tipo = TipoLancamento.Saida;

    [ObservableProperty]
    private string mensagemErro = string.Empty;

    [ObservableProperty]
    private bool estaSalvando;

    public event EventHandler? Concluido;

    public IAsyncRelayCommand SalvarCommand { get; }

    public NovaCategoriaViewModel(CategoriaService categoriaService, Guid empresaId)
    {
        _categoriaService = categoriaService;
        _empresaId = empresaId;

        SalvarCommand = new AsyncRelayCommand(SalvarAsync);
    }

    /// <summary>Modo edição: pré-preenche os campos a partir de uma categoria já existente.</summary>
    public NovaCategoriaViewModel(CategoriaService categoriaService, Guid empresaId, Categoria categoriaExistente)
        : this(categoriaService, empresaId)
    {
        _categoriaIdEmEdicao = categoriaExistente.Id;
        Nome = categoriaExistente.Nome;
        Tipo = categoriaExistente.Tipo;
    }

    private async Task SalvarAsync()
    {
        MensagemErro = string.Empty;

        if (string.IsNullOrWhiteSpace(Nome))
        {
            MensagemErro = "Informe o nome da categoria.";
            return;
        }

        EstaSalvando = true;
        try
        {
            if (_categoriaIdEmEdicao is Guid categoriaId)
                await _categoriaService.AtualizarAsync(categoriaId, Nome);
            else
                await _categoriaService.CriarAsync(_empresaId, Nome, Tipo);

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
