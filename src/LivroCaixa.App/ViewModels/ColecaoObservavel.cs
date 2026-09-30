using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace LivroCaixa.App.ViewModels;

/// <summary>
/// <see cref="ObservableCollection{T}"/> com troca de conteúdo em lote. <c>Clear()</c> seguido
/// de um <c>Add()</c> por item dispara uma notificação por item, e cada uma faz a DataGrid
/// recalcular o layout — com centenas de lançamentos isso deixava a recarga/filtragem lenta.
/// <see cref="SubstituirPor"/> troca tudo e avisa a tela uma vez só (Reset).
/// </summary>
public class ColecaoObservavel<T> : ObservableCollection<T>
{
    public void SubstituirPor(IEnumerable<T> itens)
    {
        CheckReentrancy();

        Items.Clear();
        foreach (var item in itens)
            Items.Add(item);

        OnPropertyChanged(new PropertyChangedEventArgs(nameof(Count)));
        OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }
}
