namespace DSLRNet.ViewModels;

using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows.Data;
using System.Windows.Threading;

public class ThreadSafeObservableCollection<T> : ObservableCollection<T>
{
    private readonly object syncRoot = new();

    public ThreadSafeObservableCollection()
    {
        BindingOperations.EnableCollectionSynchronization(this, syncRoot);
    }

    public override event NotifyCollectionChangedEventHandler? CollectionChanged = delegate { };

    public Dispatcher Dispatcher { get; } = System.Windows.Application.Current.Dispatcher;

    protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
    {
        NotifyCollectionChangedEventHandler? CollectionChanged = this.CollectionChanged;

        if (CollectionChanged != null)
        {
            foreach (NotifyCollectionChangedEventHandler nh in CollectionChanged.GetInvocationList())
            {
                try
                {
                    if (!Dispatcher.CheckAccess())
                    {
                        Dispatcher.BeginInvoke(() =>
                        {
                            try
                            {
                                nh.Invoke(this, e);
                            }
                            catch (Exception)
                            {
                                // nom nom we don't care if a logging thing caused an exception
                            }
                        }, DispatcherPriority.DataBind);
                    }
                    else
                    {
                        nh.Invoke(this, e);
                    }
                }
                catch (Exception)
                {
                    // nom nom we don't care if a logging thing caused an exception
                }
            }
        }
    }

    protected override void InsertItem(int index, T item)
    {
        lock (syncRoot)
        {
            base.InsertItem(index, item);
        }
    }

    protected override void RemoveItem(int index)
    {
        lock (syncRoot)
        {
            base.RemoveItem(index);
        }
    }

    protected override void SetItem(int index, T item)
    {
        lock (syncRoot)
        {
            base.SetItem(index, item);
        }
    }

    protected override void ClearItems()
    {
        lock (syncRoot)
        {
            base.ClearItems();
        }
    }
}
