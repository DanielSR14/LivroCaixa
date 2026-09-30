using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LivroCaixa.Application.Services;
using LivroCaixa.Domain.Exceptions;

namespace LivroCaixa.App.ViewModels;

public partial class ArmazenamentoViewModel : ObservableObject
{
    private readonly ArmazenamentoService _armazenamentoService;
    private readonly Guid _empresaId;

    public ObservableCollection<ItemArmazenamentoCompetencia> Competencias { get; } = new();

    public string EspacoTotalExibicao => ItemArmazenamentoCompetencia.FormatarTamanho(Competencias.Sum(c => c.TamanhoTotalBytes));

    public int TotalComprovantes => Competencias.Sum(c => c.QuantidadeAnexos);

    [ObservableProperty]
    private string mensagemErro = string.Empty;

    [ObservableProperty]
    private string mensagemStatus = string.Empty;

    public IAsyncRelayCommand CarregarCommand { get; }

    public IAsyncRelayCommand<ItemArmazenamentoCompetencia> LimparCommand { get; }

    public ArmazenamentoViewModel(ArmazenamentoService armazenamentoService, Guid empresaId)
    {
        _armazenamentoService = armazenamentoService;
        _empresaId = empresaId;

        CarregarCommand = new AsyncRelayCommand(CarregarAsync);
        LimparCommand = new AsyncRelayCommand<ItemArmazenamentoCompetencia>(LimparAsync!);
    }

    public Task InicializarAsync() => CarregarAsync();

    private async Task CarregarAsync()
    {
        MensagemErro = string.Empty;
        try
        {
            var resumo = await _armazenamentoService.ListarResumoPorCompetenciaAsync(_empresaId);
            Competencias.Clear();
            foreach (var item in resumo)
            {
                Competencias.Add(new ItemArmazenamentoCompetencia(
                    item.Ano, item.Mes, item.QuantidadeLancamentos, item.QuantidadeAnexos,
                    item.TamanhoTotalBytes, item.JaExportada));
            }

            OnPropertyChanged(nameof(EspacoTotalExibicao));
            OnPropertyChanged(nameof(TotalComprovantes));
        }
        catch (DomainException ex)
        {
            MensagemErro = ex.Message;
        }
    }

    private async Task LimparAsync(ItemArmazenamentoCompetencia item)
    {
        MensagemErro = string.Empty;
        MensagemStatus = string.Empty;
        try
        {
            var quantidadeRemovida = await _armazenamentoService.LimparAnexosDeCompetenciaAsync(_empresaId, item.Ano, item.Mes);
            MensagemStatus = quantidadeRemovida > 0
                ? $"{quantidadeRemovida} comprovante(s) removido(s) da competência {item.CompetenciaExibicao}."
                : $"Não havia comprovantes para remover na competência {item.CompetenciaExibicao}.";

            await CarregarAsync();
        }
        catch (DomainException ex)
        {
            MensagemErro = ex.Message;
        }
    }
}
