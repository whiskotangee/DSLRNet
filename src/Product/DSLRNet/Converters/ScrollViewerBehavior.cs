using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;

namespace DSLRNet.Behaviors
{
    public static class ScrollViewerBehavior
    {
        public static readonly DependencyProperty AutoScrollProperty =
            DependencyProperty.RegisterAttached("AutoScroll", typeof(bool), typeof(ScrollViewerBehavior), new PropertyMetadata(false, OnAutoScrollChanged));

        public static bool GetAutoScroll(DependencyObject obj)
        {
            return (bool)obj.GetValue(AutoScrollProperty);
        }

        public static void SetAutoScroll(DependencyObject obj, bool value)
        {
            obj.SetValue(AutoScrollProperty, value);
        }

        private static void OnAutoScrollChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ItemsControl itemsControl)
            {
                if ((bool)e.NewValue && itemsControl.ItemsSource != null)
                {
                    ((INotifyCollectionChanged)itemsControl.ItemsSource).CollectionChanged += (s, ev) =>
                    {
                        if (itemsControl.ItemContainerGenerator.ContainerFromIndex(itemsControl.Items.Count - 1) is FrameworkElement lastItem)
                        {
                            lastItem.BringIntoView();
                        }
                    };
                }
            }
        }
    }
}
