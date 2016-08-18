using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace WpfMultiselectTreeViewKit.Utils
{
    public static class ScrollOnDragDropExtension
    {
        #region ScrollOnDragDropProperty
 
        public static readonly DependencyProperty ScrollOnDragDropProperty =
            DependencyProperty.RegisterAttached("ScrollOnDragDrop",
                typeof(bool),
                typeof(ScrollOnDragDropExtension),
                new PropertyMetadata(false, HandleScrollOnDragDropChanged));
 
        public static bool GetScrollOnDragDrop(DependencyObject element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }
 
            return (bool)element.GetValue(ScrollOnDragDropProperty);
        }
 
        public static void SetScrollOnDragDrop(DependencyObject element, bool value)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }
 
            element.SetValue(ScrollOnDragDropProperty, value);
        }
 
        private static void HandleScrollOnDragDropChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            FrameworkElement container = d as FrameworkElement;
 
            if (d == null)
            {
                Debug.Fail("Invalid type!");
                return;
            }
 
            Unsubscribe(container);
 
            if (true.Equals(e.NewValue))
            {
                Subscribe(container);
            }
        }
 
        private static void Subscribe(FrameworkElement container)
        {
            container.PreviewDragOver += OnContainerPreviewDragOver;
        }
 
        private static void OnContainerPreviewDragOver(object sender, DragEventArgs e)
        {
            FrameworkElement container = sender as FrameworkElement;
 
            if (container == null)
            {
                return;
            }
 
            ScrollViewer scrollViewer = GetFirstVisualChild<ScrollViewer>(container);
 
            if (scrollViewer == null)
            {
                return;
            }
 
            double tolerance = 60;
            double verticalPos = e.GetPosition(container).Y;
            double offset = GetScrollOffset(container);
 
            if (verticalPos < tolerance) // Top of visible list? 
            {
                scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset - offset); //Scroll up. 
            }
            else if (verticalPos > container.ActualHeight - tolerance) //Bottom of visible list? 
            {
                scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset + offset); //Scroll down.     
            }
        }
 
        private static void Unsubscribe(FrameworkElement container)
        {
            container.PreviewDragOver -= OnContainerPreviewDragOver;
        }
 
        public static T GetFirstVisualChild<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj != null)
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
                    if (child != null && child is T)
                    {
                        return (T)child;
                    }
 
                    T childItem = GetFirstVisualChild<T>(child);
                    if (childItem != null)
                    {
                        return childItem;
                    }
                }
            }
 
            return null;
        }
 
        #endregion

        #region XX

        public static readonly DependencyProperty ScrollOffsetProperty =
            DependencyProperty.RegisterAttached("ScrollOffset",
                typeof(int),
                typeof(ScrollOnDragDropExtension),
                new PropertyMetadata(40));

        public static int GetScrollOffset(DependencyObject element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            return (int)element.GetValue(ScrollOffsetProperty);
        }

        public static void SetScrollOffset(DependencyObject element, int value)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            element.SetValue(ScrollOffsetProperty, value);
        }
        #endregion

    }
}