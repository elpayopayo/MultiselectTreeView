using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using WpfMultiselectTreeViewKit.Interfaces;
using Prism.Mvvm;

namespace WpfMultiselectTreeViewKit.UserControl
{
    public class DroppedData
    {
        public DroppedData(object target, IDataObject source, DragDropEffects mode)
        {
            Target = target;
            Source = source;
            Mode = mode;
        }

        public object Target { get; private set; }
        public IDataObject Source { get; private set; }
        public DragDropEffects Mode { get; private set; }
    }
    public abstract class AsyncWpfTreeViewControlBase : TreeView
    {
        //TODO: must implement: ShowRootLines, HideSelection and LabelEdit

        #region Dependency Properties

        public static DependencyProperty AllowEditItemsProperty = DependencyProperty.Register(
            "AllowEditItems",
            typeof (bool),
            typeof (AsyncWpfTreeViewControlBase),
            new FrameworkPropertyMetadata(false, null));

        public static readonly DependencyProperty EnableMultiSelectProperty =
            DependencyProperty.RegisterAttached("EnableMultiSelect", typeof (bool),
                typeof(AsyncWpfTreeViewControlBase), new FrameworkPropertyMetadata(false)
                {
                    BindsTwoWayByDefault = true
                });

        #endregion

        protected AsyncWpfTreeViewControlBase()
        {
            EnableMultiSelect();
            SelectedTreeViewNodes = new List<ITreeViewNode<ITreeViewNodeValue>>();
            DataContextChanged += OnDataContextChanged;
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            base.OnMouseDown(e);
            Focus();
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs args)
        {
            SelectedTreeViewNodes.Clear();
            var viewModel = DataContext as ITreeView<ITreeViewNodeValue>;
            if (ViewModel != null)
            {
                ViewModel.PropertyChanged -= ViewModelOnPropertyChanged;
            }
            ViewModel = viewModel;
            if(ViewModel!=null)
            {
                ViewModel.PropertyChanged+=ViewModelOnPropertyChanged;
            }
        }

        private void ViewModelOnPropertyChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
        {
            if (propertyChangedEventArgs.PropertyName == PropertySupport.ExtractPropertyName(()=>ViewModel.Items))
            {
                SelectedTreeViewNodes.Clear();
            }
        }

        public ITreeView<ITreeViewNodeValue> ViewModel
        {
            get { return mViewModel; }
            private set { mViewModel = value; }
        }

        public event EventHandler SelectionChanged;

        public bool AllowEditItems
        {
            get { return (bool) GetValue(AllowEditItemsProperty); }
            set { SetValue(AllowEditItemsProperty, value); }
        }

        #region Overrides

        private ITreeView<ITreeViewNodeValue> mViewModel;

        public AsyncWpfTreeViewItem CreateTreeViewItem()
        {
            var newItem = new AsyncWpfTreeViewItem(this);
            newItem.MouseLeftButtonDown += ItemClicked;

            newItem.MouseLeftButtonUp += (sender, args) =>
            {
                EndDragDrop();
            };
            newItem.MouseRightButtonDown += ItemRightClicked;
            newItem.QueryContinueDrag += (sender, args) =>
            {
                if (args.EscapePressed || args.Action == DragAction.Cancel || args.Action == DragAction.Drop
                    || args.KeyStates == DragDropKeyStates.None)
                {
                    EndDragDrop();
                }
            };
            newItem.MultiSelectChanged += NewItemOnMultiSelectChanged;
            //newItem.GotFocus += ItemGotFocus;
            return newItem;
        }

        private void NewItemOnMultiSelectChanged(object sender, RoutedEventArgs routedEventArgs)
        {
            var newItem = sender as AsyncWpfTreeViewItem;
            if (newItem == null) return;
            var node = newItem.DataContext as ITreeViewNode<ITreeViewNodeValue>;
            if (node == null) return;
            var existingItem = SelectedTreeViewNodes.FirstOrDefault(item => item.CompareTo(newItem.DataContext)==0);
            if (node.IsMultiSelected)
            {
                if (existingItem==null)
                {
                    SelectedTreeViewNodes.Add(node);
                }
            }
            else
            {
                if(existingItem!=null)
                    SelectedTreeViewNodes.Remove(existingItem);
            }
            routedEventArgs.Handled = true;
            if (node.IsMultiSelected)
            {
                OnSelectionChanged();
            }
        }

        protected override DependencyObject GetContainerForItemOverride()
        {
            return CreateTreeViewItem();
        }

        protected override bool IsItemItsOwnContainerOverride(object item)
        {
            return item is AsyncWpfTreeViewItem;
        }

        #endregion
        
        #region Multi Selection

        private void EnableMultiSelect()
        {
            //tree.SelectedItemChanged += TreeOnSelectedItemChanged;
            AddHandler(TreeViewItem.SelectedEvent, new RoutedEventHandler(TreeOnSelectedItemChanged));
            //AddHandler(TreeViewItem.CollapsedEvent, new RoutedEventHandler(TreeViewItemOnExpandCollapse));
            //AddHandler(TreeViewItem.ExpandedEvent, new RoutedEventHandler(TreeViewItemOnExpandCollapse));
            AddHandler(KeyDownEvent, new KeyEventHandler(OnKeyDown));
        }

        //private IList<AsyncWpfTreeViewItem> SelectedTreeViewItems { get; set; }
        private IList<ITreeViewNode<ITreeViewNodeValue>> SelectedTreeViewNodes { get; set; } 

        public IList SelectedItems
        {
            //get { return SelectedTreeViewItems!=null?SelectedTreeViewItems.Where(item=>item.IsMultiSelected || item.IsSelected).Select(item=>item.DataContext).ToList() : null; }
            get { return SelectedTreeViewNodes != null ? SelectedTreeViewNodes.Where(item => item.IsMultiSelected || item.IsSelected).ToList() : null; }
        }
        
        protected async void OnSelectionChanged()
        {
            var handler = SelectionChanged;
            if (handler != null)
            {
                var t = Task.Run(() => Dispatcher.InvokeAsync(()=> handler(this, EventArgs.Empty)));
                await t;
            }
        }

        private void TreeOnSelectedItemChanged(object sender, RoutedEventArgs e)
        {
            var item = e.OriginalSource as AsyncWpfTreeViewItem;
            if (item != null)
            {
                item.BringIntoView();
                if (item.IsLoadingData)
                {
                    if (PreviousSelected != null)
                    {
                        PreviousSelected.IsMultiSelected = false;
                    }
                    MakeToggleSelection(item);
                }
                else
                {
                    ApplyMultiSelection(item);
                }
                item.IsLoadingData = false;
                //OnSelectionChanged();
                PreviousSelected = item.DataContext as ITreeViewNode<ITreeViewNodeValue>;
                e.Handled = true;
            }
        }
        private ITreeViewNode<ITreeViewNodeValue> PreviousSelected { get; set; } 
        
        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            TreeView tree = (TreeView)sender;
            if (e.Key == Key.A && e.KeyboardDevice.Modifiers == ModifierKeys.Control)
            {
                foreach (var item in GetExpandedTreeViewItems(tree))
                {
                    item.SetSilentMultiSelected(true);
                    //SelectedTreeViewItems.Add(item);
                }
                e.Handled = true;
            }
        }

        private void ItemRightClicked(object sender, MouseButtonEventArgs e)
        {
            var item = sender as AsyncWpfTreeViewItem;
            if (item == null)
                return;
            var node = item.DataContext as ITreeViewNode<ITreeViewNodeValue>;
            if (node == null) return;
            if (!node.IsMultiSelected)
            {
                node.IsSelected = true;
                MakeSingleSelection(item);
            }
            else
            {
                OnSelectionChanged();
            }
            e.Handled = true;
        }

        private void ItemClicked(object sender, MouseButtonEventArgs e)
        {
            //var item = sender as AsyncWpfTreeViewItem;
            //if (item == null)
            //    return;
            //if (item == mLastExpandedCollapsedItem)
            //{
            //    mLastExpandedCollapsedItem = null;
            //    return;
            //}
            //ApplyMultiSelection(item);
            e.Handled = true;
        }

        private void ItemGotFocus(object sender, RoutedEventArgs e)
        {
            var item = sender as AsyncWpfTreeViewItem;
            if (item == null)
                return;
            item.IsSelected = true;
            MakeToggleSelection(item);
            e.Handled = true;
        }

        private List<AsyncWpfTreeViewItem> GetAllNodes(Visual item)
        {
            if (item == null)
                return null;

            var result = new List<AsyncWpfTreeViewItem>();

            var frameworkElement = item as FrameworkElement;
            if (frameworkElement != null)
            {
                frameworkElement.ApplyTemplate();
            }

            Visual child = null;
            for (int i = 0, count = VisualTreeHelper.GetChildrenCount(item); i < count; i++)
            {
                child = VisualTreeHelper.GetChild(item, i) as Visual;

                var treeViewItem = child as AsyncWpfTreeViewItem;
                if (treeViewItem != null)
                {
                    result.Add(treeViewItem);
                }
                foreach (var childTreeViewItem in GetAllNodes(child))
                {
                    result.Add(childTreeViewItem);
                }
            }
            return result;
        }

        private void ApplyMultiSelection(AsyncWpfTreeViewItem item)
        {
            if ((Keyboard.Modifiers & (ModifierKeys.Shift | ModifierKeys.Control)) != (ModifierKeys.Shift | ModifierKeys.Control))
            {
                if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                {
                    MakeToggleSelection(item);
                    return;
                }
                //TODO: shift temporarily disabled due tue bug 114521
                //if ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
                //{
                //    MakeToggleSelection(item);
                //    return;
                //}
                MakeSingleSelection(item);
            }
        }

        private IEnumerable<AsyncWpfTreeViewItem> GetExpandedTreeViewItems(ItemsControl tree)
        {
            for (int i = 0; i < tree.Items.Count; i++)
            {
                var item = (AsyncWpfTreeViewItem)tree.ItemContainerGenerator.ContainerFromIndex(i);
                if (item == null)
                    continue;
                yield return item;
                if (item.IsExpanded)
                    foreach (var subItem in GetExpandedTreeViewItems(item))
                        yield return subItem;
            }
        }

        private List<AsyncWpfTreeViewItem> GetSelectedTreeViewItems()
        {
            return GetExpandedTreeViewItems(this).Where(item=>item.IsMultiSelected).ToList();
        }

        private void MakeSingleSelection(AsyncWpfTreeViewItem item)
        {
            var currentSelectedItems = SelectedTreeViewNodes.ToList();
            foreach (var node in currentSelectedItems)
            {
                node.IsMultiSelected = false;
            }
            SelectedTreeViewNodes.Clear();
            var treeViewNode = item.DataContext as ITreeViewNode<ITreeViewNodeValue>;
            if (treeViewNode != null)
            {
                treeViewNode.IsMultiSelected = true;
            }
            //SelectedTreeViewItems.Add(item);
            //OnSelectionChanged();
            //UpdateAnchorAndActionItem(item);
        }

        private void MakeToggleSelection(AsyncWpfTreeViewItem item)
        {
            var node = item.DataContext as ITreeViewNode<ITreeViewNodeValue>;
            if (node == null) return;
            node.IsMultiSelected = !node.IsMultiSelected;
            //if (item.IsMultiSelected)
            //{
            //    SelectedTreeViewItems.Add(item);
            //}
            //else
            //{
            //    SelectedTreeViewItems.Remove(item);
            //}
            if (node.IsSelected)
            {
                //item.IsSelected = false;
                Focus();
                item.Focus();
            }
            //OnSelectionChanged();
            //UpdateAnchorAndActionItem(item);
        }

        
        #endregion

        internal void DoDragDrop(AsyncWpfTreeViewItem item)
        {
            EndDragDrop();
            var viewModel = DataContext as IDragEnabledTreeView;
            if (viewModel == null) return;
            BeginDrag();
            viewModel.DoDragDrop(item);
        }

        internal void DoDrop(DroppedData data)
        {
            var viewModel = DataContext as IDragEnabledTreeView;
            if (viewModel == null) return;
            viewModel.DoDrop(data);
        }

        internal bool CanDrop(IDataObject source, AsyncWpfTreeViewItem target)
        {
            var viewModel = DataContext as IDragEnabledTreeView;
            if (viewModel == null) return false;
            return viewModel.CanDrop(source, target.DataContext as ITreeViewNode<ITreeViewNodeValue>);
        }

        internal void BeginDrag()
        {
            var viewModel = DataContext as ITreeView<ITreeViewNodeValue>;
            if (viewModel == null || viewModel.SelectedItems == null) return;
            foreach(var item in viewModel.SelectedItems)
            {
                item.IsBeingDragged = true;
            }
        }

        internal void EndDragDrop()
        {
            var viewModel = DataContext as ITreeView<ITreeViewNodeValue>;
            if (viewModel == null) return;
            foreach (var item in viewModel.GetAllNodes())
            {
                item.IsBeingDragged = false;
            }
        }

    }
}