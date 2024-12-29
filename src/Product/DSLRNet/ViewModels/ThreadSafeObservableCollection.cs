namespace DSLRNet.ViewModels;

using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows.Threading;

public class ThreadSafeObservableCollection<T> : ObservableCollection<T>
{
    public override event NotifyCollectionChangedEventHandler CollectionChanged;
    protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
    {
        NotifyCollectionChangedEventHandler CollectionChanged = this.CollectionChanged;

        if (CollectionChanged != null)
        {
            foreach (NotifyCollectionChangedEventHandler nh in CollectionChanged.GetInvocationList())
            {
                if (nh.Target is DispatcherObject dispObj)
                {
                    Dispatcher dispatcher = dispObj.Dispatcher;
                    if (dispatcher != null && !dispatcher.CheckAccess())
                    {
                        try
                        {
                            dispatcher.BeginInvoke(() =>
                            {
                                try 
                                {
                                    nh.Invoke(this, e); 
                                } 
                                catch (Exception ex) 
                                {
                                    // nom nom we don't care if a logging thing caused an exception
                                }
                            }, DispatcherPriority.DataBind);
                        }
                        catch (Exception ex)
                        {
                            // nom nom we don't care if a logging thing caused an exception
                        }

                        continue;
                    }
                }
                try
                {
                    nh.Invoke(this, e);
                }
                catch (Exception)
                {
                    // nom nom we don't care if a logging thing caused an exception
                }
            }
        }
    }
}
