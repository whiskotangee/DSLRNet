namespace DSLRNet.ViewModels;

using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows.Threading;

public class ThreadSafeObservableCollection<T> : ObservableCollection<T>
{
    public override event NotifyCollectionChangedEventHandler? CollectionChanged = delegate { };

    protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
    {
        NotifyCollectionChangedEventHandler? CollectionChanged = this.CollectionChanged;

        if (CollectionChanged != null)
        {
            foreach (NotifyCollectionChangedEventHandler nh in CollectionChanged.GetInvocationList())
            {
                try
                {
                    if (nh.Target is DispatcherObject dispObj)
                    {
                        Dispatcher dispatcher = dispObj.Dispatcher;
                        if (dispatcher != null && !dispatcher.CheckAccess())
                        {
                            dispatcher.Invoke(() =>
                            {
                                nh.Invoke(this, e);
                            }, DispatcherPriority.DataBind);
                        }
                        else
                        {
                            nh.Invoke(this, e);
                        }
                    }
                }
                catch (Exception)
                {
                    // nom nom we don't care if a logging thing caused an exception
                }
            }
        }
    }
}
