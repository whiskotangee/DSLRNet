namespace DSLRNet.Common;

using System.Collections.Concurrent;

using System.Collections.Specialized;
using System.ComponentModel;

public class ThreadSafeObservableCollection<T> : INotifyCollectionChanged, INotifyPropertyChanged
{
    private readonly ConcurrentBag<T> collection;
    private readonly SynchronizationContext synchronizationContext;

    public event PropertyChangedEventHandler? PropertyChanged;
    public event NotifyCollectionChangedEventHandler? CollectionChanged;

    public ThreadSafeObservableCollection()
    {
        collection = [];
        synchronizationContext = SynchronizationContext.Current ?? new SynchronizationContext();
    }

    public ConcurrentBag<T> UnderlyingCollection => collection;

    protected void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
    {
        if (SynchronizationContext.Current == synchronizationContext)
        {
            CollectionChanged?.Invoke(this, e);
        }
        else
        {
            synchronizationContext.Post(_ => CollectionChanged?.Invoke(this, e), null);
        }
    }

    protected void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        if (SynchronizationContext.Current == synchronizationContext)
        {
            PropertyChanged?.Invoke(this, e);
        }
        else
        {
            synchronizationContext.Post(_ => PropertyChanged?.Invoke(this, e), null);
        }
    }

    public void Add(T item)
    {
        collection.Add(item);
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item));
        OnPropertyChanged(new PropertyChangedEventArgs(nameof(Count)));
    }

    public void Clear()
    {
        while (!collection.IsEmpty)
        {
            collection.TryTake(out _);
        }
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        OnPropertyChanged(new PropertyChangedEventArgs(nameof(Count)));
    }

    public bool Remove(T item)
    {
        var items = collection.ToList();
        if (items.Remove(item))
        {
            collection.Clear();
            foreach (var remainingItem in items)
            {
                collection.Add(remainingItem);
            }
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item));
            OnPropertyChanged(new PropertyChangedEventArgs(nameof(Count)));
            return true;
        }
        return false;
    }

    public int Count => collection.Count;

    public IEnumerator<T> GetEnumerator() => collection.GetEnumerator();
}
