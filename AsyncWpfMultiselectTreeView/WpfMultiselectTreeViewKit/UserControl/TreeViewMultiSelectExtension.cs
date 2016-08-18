//using System;
//using System.Collections;
//using System.Collections.Generic;
//using System.Linq;
//using System.Windows;
//using System.Windows.Controls;
//using System.Windows.Input;
//using System.Windows.Media;

//namespace WpfMultiselectTreeViewKit.UserControl
//{
//    public class TreeViewMultiSelectExtension : DependencyObject
//    {
//        public static bool GetEnableMultiSelect(DependencyObject obj)
//        {
//            return (bool)obj.GetValue(EnableMultiSelectProperty);
//        }

//        public static void SetEnableMultiSelect(DependencyObject obj, bool value)
//        {
//            obj.SetValue(EnableMultiSelectProperty, value);
//        }

//        // Using a DependencyProperty as the backing store for EnableMultiSelect.  This enables animation, styling, binding, etc...
//        public static readonly DependencyProperty EnableMultiSelectProperty =
//            DependencyProperty.RegisterAttached("EnableMultiSelect", typeof(bool), typeof(TreeViewMultiSelectExtension), new FrameworkPropertyMetadata(false)
//            {
//                PropertyChangedCallback = EnableMultiSelectChanged,
//                BindsTwoWayByDefault = true
//            });


//        public static IList GetSelectedItems(AsyncWpfTreeViewControl tree)
//        {
//            return tree == null ? null : tree.SelectedItems;
//        }

//        public static void SetSelectedItems(AsyncWpfTreeViewControl tree, IList value)
//        {
//            if (tree == null) return;
//            tree.SelectedItems = value;
//        }

//        static TreeViewItem GetAnchorItem(DependencyObject obj)
//        {
//            return (TreeViewItem)obj.GetValue(AnchorItemProperty);
//        }

//        static void SetAnchorItem(DependencyObject obj, TreeViewItem value)
//        {
//            obj.SetValue(AnchorItemProperty, value);
//        }

//        // Using a DependencyProperty as the backing store for AnchorItem.  This enables animation, styling, binding, etc...
//        static readonly DependencyProperty AnchorItemProperty =
//            DependencyProperty.RegisterAttached("AnchorItem", typeof(TreeViewItem), typeof(TreeViewMultiSelectExtension), new PropertyMetadata(null));



//        static void EnableMultiSelectChanged(DependencyObject s, DependencyPropertyChangedEventArgs args)
//        {
//            TreeView tree = (TreeView)s;
//            var wasEnable = (bool)args.OldValue;
//            var isEnabled = (bool)args.NewValue;
//            if (wasEnable)
//            {
//                //tree.SelectedItemChanged -= TreeOnSelectedItemChanged;
//                tree.RemoveHandler(TreeViewItem.SelectedEvent, new RoutedEventHandler(TreeOnSelectedItemChanged));
//                tree.RemoveHandler(TreeViewItem.CollapsedEvent, new RoutedEventHandler(TreeViewItemOnExpandCollapse));
//                tree.RemoveHandler(TreeViewItem.ExpandedEvent, new RoutedEventHandler(TreeViewItemOnExpandCollapse));
//                tree.RemoveHandler(TreeViewItem.MouseLeftButtonUpEvent, new MouseButtonEventHandler(ItemClicked));
//                tree.RemoveHandler(TreeViewItem.MouseRightButtonUpEvent, new MouseButtonEventHandler(ItemClicked));
//                tree.RemoveHandler(TreeView.KeyDownEvent, new KeyEventHandler(KeyDown));
//            }
//            if (isEnabled)
//            {
//                //tree.SelectedItemChanged += TreeOnSelectedItemChanged;
//                tree.AddHandler(TreeViewItem.SelectedEvent, new RoutedEventHandler(TreeOnSelectedItemChanged));
//                tree.AddHandler(TreeViewItem.CollapsedEvent, new RoutedEventHandler(TreeViewItemOnExpandCollapse));
//                tree.AddHandler(TreeViewItem.ExpandedEvent, new RoutedEventHandler(TreeViewItemOnExpandCollapse));
//                tree.AddHandler(TreeViewItem.MouseLeftButtonUpEvent, new MouseButtonEventHandler(ItemClicked), true);
//                tree.AddHandler(TreeViewItem.MouseRightButtonUpEvent, new MouseButtonEventHandler(ItemClicked), true);
//                tree.AddHandler(TreeView.KeyDownEvent, new KeyEventHandler(KeyDown));
//            }
//        }

//        static void TreeOnSelectedItemChanged(object sender, RoutedEventArgs e)
//        {
//            var tree = sender as AsyncWpfTreeViewControl;
//            if (tree == null) return;
//            TreeViewItem item =e.OriginalSource as TreeViewItem;
//            if (item == null)
//                return;
//            OnLeftMouseButtonUp(tree, item);
//        }

//        private static TreeViewItem lastExpandedCollapsedItem;
//        private static void TreeViewItemOnExpandCollapse(object sender, RoutedEventArgs e)
//        {
//            lastExpandedCollapsedItem = e.OriginalSource as TreeViewItem;
//        }

//        static AsyncWpfTreeViewControl GetTree(TreeViewItem item)
//        {
//            Func<DependencyObject, DependencyObject> getParent = VisualTreeHelper.GetParent;
//            FrameworkElement currentItem = item;
//            while (!(getParent(currentItem) is TreeView))
//                currentItem = (FrameworkElement)getParent(currentItem);
//            return getParent(currentItem) as AsyncWpfTreeViewControl;
//        }

//        static void RealSelectedChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
//        {
//            TreeViewItem item = (TreeViewItem)sender;
//            var tree = GetTree(item);
//            if (tree == null) return;
//            var selectedItems = GetSelectedItems(tree);
//            if (selectedItems == null)
//            {
//                selectedItems = new List<object>();
//            }
//            var isSelected = GetIsMultiSelected(item);
//            if (isSelected)
//                try
//                {
//                    selectedItems.Add(item.Header);
//                }
//                catch (ArgumentException)
//                {
//                }
//            else
//                selectedItems.Remove(item.Header);
//            SetSelectedItems(tree, selectedItems);
//        }

//        static void KeyDown(object sender, KeyEventArgs e)
//        {
//            TreeView tree = (TreeView)sender;
//            if (e.Key == Key.A && e.KeyboardDevice.Modifiers == ModifierKeys.Control)
//            {
//                foreach (var item in GetExpandedTreeViewItems(tree))
//                {
//                    SetIsMultiSelected(item, true);
//                }
//                e.Handled = true;
//            }
//        }

//        static void ItemClicked(object sender, MouseButtonEventArgs e)
//        {
//            TreeViewItem item = FindTreeViewItem(e.OriginalSource);
//            if (item == null)
//                return;
//            var tree = (AsyncWpfTreeViewControl)sender;

//            if (e.ChangedButton == MouseButton.Right)
//            {
//                if (!GetIsMultiSelected(item))
//                {
//                    MakeSingleSelection(tree, item);
//                    item.IsSelected = true;
//                }
//                return;
//            }
//            OnLeftMouseButtonUp(tree, item);
//        }

//        private static void OnLeftMouseButtonUp(AsyncWpfTreeViewControl tree, TreeViewItem item)
//        {
//            if (item == lastExpandedCollapsedItem)
//            {
//                lastExpandedCollapsedItem = null;
//                return;
//            }
//            if ((Keyboard.Modifiers & (ModifierKeys.Shift | ModifierKeys.Control)) != (ModifierKeys.Shift | ModifierKeys.Control))
//            {
//                if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
//                {
//                    MakeToggleSelection(tree, item);
//                    return;
//                }
//                if ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
//                {
//                    MakeAnchorSelection(tree, item, true);
//                    return;
//                }
//                MakeSingleSelection(tree, item);
//            }
//        }

//        private static TreeViewItem FindTreeViewItem(object obj)
//        {
//            DependencyObject dpObj = obj as DependencyObject;
//            if (dpObj == null)
//                return null;
//            if (dpObj is TreeViewItem)
//                return (TreeViewItem)dpObj;
//            return FindTreeViewItem(VisualTreeHelper.GetParent(dpObj));
//        }



//        private static IEnumerable<TreeViewItem> GetExpandedTreeViewItems(ItemsControl tree)
//        {
//            for (int i = 0; i < tree.Items.Count; i++)
//            {
//                var item = (TreeViewItem)tree.ItemContainerGenerator.ContainerFromIndex(i);
//                if (item == null)
//                    continue;
//                yield return item;
//                if (item.IsExpanded)
//                    foreach (var subItem in GetExpandedTreeViewItems(item))
//                        yield return subItem;
//            }
//        }

//        private static void MakeAnchorSelection(TreeView tree, TreeViewItem actionItem, bool clearCurrent)
//        {
//            if (GetAnchorItem(tree) == null)
//            {
//                var selectedItems = GetSelectedTreeViewItems(tree);
//                if (selectedItems.Count > 0)
//                {
//                    SetAnchorItem(tree, selectedItems[selectedItems.Count - 1]);
//                }
//                else
//                {
//                    SetAnchorItem(tree, GetExpandedTreeViewItems(tree).Skip(3).FirstOrDefault());
//                }
//                if (GetAnchorItem(tree) == null)
//                {
//                    return;
//                }
//            }

//            var anchor = GetAnchorItem(tree);

//            var items = GetExpandedTreeViewItems(tree);
//            bool betweenBoundary = false;
//            foreach (var item in items)
//            {
//                bool isBoundary = item == anchor || item == actionItem;
//                if (isBoundary)
//                {
//                    betweenBoundary = !betweenBoundary;
//                }
//                if (betweenBoundary || isBoundary)
//                    SetIsMultiSelected(item, true);
//                else
//                    if (clearCurrent)
//                        SetIsMultiSelected(item, false);
//                    else
//                        break;

//            }
//        }

//        private static List<TreeViewItem> GetSelectedTreeViewItems(TreeView tree)
//        {
//            return GetExpandedTreeViewItems(tree).Where(GetIsMultiSelected).ToList();
//        }

//        private static void MakeSingleSelection(TreeView tree, TreeViewItem item)
//        {
//            foreach (TreeViewItem selectedItem in GetExpandedTreeViewItems(tree))
//            {
//                if (selectedItem == null)
//                {
//                    continue;
//                }
//                SetIsMultiSelected(selectedItem, selectedItem == item);
//            }
//            UpdateAnchorAndActionItem(tree, item);
//        }

//        private static void MakeToggleSelection(TreeView tree, TreeViewItem item)
//        {
//            SetIsMultiSelected(item, !GetIsMultiSelected(item));
//            UpdateAnchorAndActionItem(tree, item);
//        }

//        private static void UpdateAnchorAndActionItem(TreeView tree, TreeViewItem item)
//        {
//            SetAnchorItem(tree, item);
//        }


//        public static bool GetIsMultiSelected(DependencyObject obj)
//        {
//            return (bool)obj.GetValue(IsMultiSelectedProperty);
//        }

//        public static void SetIsMultiSelected(DependencyObject obj, bool value)
//        {
//            obj.SetValue(IsMultiSelectedProperty, value);
//        }

//        // Using a DependencyProperty as the backing store for IsSelected.  This enables animation, styling, binding, etc...
//        public static readonly DependencyProperty IsMultiSelectedProperty =
//            DependencyProperty.RegisterAttached("IsMultiSelected", typeof(bool), typeof(TreeViewMultiSelectExtension), new PropertyMetadata(false)
//            {
//                PropertyChangedCallback = RealSelectedChanged
//            });


//    }
//}
