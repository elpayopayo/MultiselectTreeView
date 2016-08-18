using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using WpfMultiselectTreeViewKit.Interfaces;
using Prism.Commands;

namespace WpfMultiselectTreeViewKit.UserControl
{
    public class AsyncWpfTreeViewItem : TreeViewItem
    {
        public AsyncWpfTreeViewControlBase OwnerTree { get; private set; }
        
        #region Dependency Properties

        public static readonly DependencyProperty IsMultiSelectedProperty =
            DependencyProperty.RegisterAttached("IsMultiSelected", typeof(bool), typeof(AsyncWpfTreeViewItem),
                new PropertyMetadata(false, OnIsMultiSelectChanged));

        public static readonly DependencyProperty IsEditableProperty =
            DependencyProperty.Register("IsEditable", typeof(bool), typeof(AsyncWpfTreeViewItem), 
            new PropertyMetadata(true));
        
        public static readonly DependencyProperty IsEditingProperty =
            DependencyProperty.Register("IsEditing", typeof (bool), typeof (AsyncWpfTreeViewItem),
                new PropertyMetadata(false));

        public static readonly DependencyProperty AllowDragProperty =
            DependencyProperty.Register("AllowDrag", typeof(bool), typeof(AsyncWpfTreeViewItem), 
            new PropertyMetadata(true));

        public static readonly DependencyProperty IsBeingDraggedProperty =
            DependencyProperty.Register("IsBeingDragged", typeof(bool), typeof(AsyncWpfTreeViewItem), 
            new PropertyMetadata(false));

        public static readonly DependencyProperty HasVisibleItemsProperty =
            DependencyProperty.Register("HasVisibleItems", typeof(bool), typeof(AsyncWpfTreeViewItem),
            new PropertyMetadata(true));

        public static readonly DependencyProperty IsExpandedExtendedProperty =
            DependencyProperty.Register("IsExpandedExtended", typeof(bool), typeof(AsyncWpfTreeViewItem),
            new PropertyMetadata(false));

        #endregion

        #region Constructor

        public AsyncWpfTreeViewItem(AsyncWpfTreeViewControlBase ownerTree)
        {
            if (ownerTree == null) throw new ArgumentNullException("ownerTree");
            OwnerTree = ownerTree;
            AllowDrop = true;
            Selected += (sender, args) => BringIntoView();
            DataContextChanged += (sender, args) =>
            {
                var dataContext = DataContext as ITreeViewNode<ITreeViewNodeValue>;
                if (dataContext == null) return;
                IsLoadingData = dataContext.IsSelected;
            }; 
            ownerTree.PreviewMouseMove += OwnerTreePreviewMouseMove;
        }

        ~AsyncWpfTreeViewItem()
        {
            if (OwnerTree != null)
            {
                OwnerTree.PreviewMouseDown -= OwnerTreePreviewMouseMove;
            }
        }
        
        #endregion


        private static void OnIsMultiSelectChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
        {
            var item = dependencyObject as AsyncWpfTreeViewItem;
            if (item == null) return;
            item.RaiseEvent(new RoutedEventArgs(MultiSelectedChangedEvent));
        }

        public static readonly RoutedEvent MultiSelectedChangedEvent = EventManager.RegisterRoutedEvent(
        "MultiSelectedChanged", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(AsyncWpfTreeViewItem));

        public event RoutedEventHandler MultiSelectChanged
        {
            add { AddHandler(MultiSelectedChangedEvent, value); }
            remove { RemoveHandler(MultiSelectedChangedEvent, value); }
        }

        private void OnMultiSelectedChanged()
        {
            RoutedEventArgs newEventArgs = new RoutedEventArgs(MultiSelectedChangedEvent);
            RaiseEvent(newEventArgs);
        }
        #region Dependency Property Setters

        public ICommand ExpanderClick
        {
            get { return mExpanderClick ?? (mExpanderClick = new DelegateCommand(OnExpanderClick)); }
        }

        private void OnExpanderClick()
        {
            var count = VisualTreeHelper.GetChildrenCount(this);

            for (int i = count - 1; i >= 0; --i)
            {
                var childItem = VisualTreeHelper.GetChild(this, i);
                ((FrameworkElement) childItem).BringIntoView();
            }
        }

        internal bool IsLoadingData { get; set; }
        public bool IsMultiSelected
        {
            get { return (bool) GetValue(IsMultiSelectedProperty); }
            set
            {
                SetValue(IsMultiSelectedProperty, value);
            }
        }

        internal void SetSilentMultiSelected(bool value)
        {
            SetValue(IsMultiSelectedProperty, value);
        }

        public bool IsEditing
        {
            get { return (bool) GetValue(IsEditingProperty); }
            set { SetValue(IsEditingProperty, value); }
        }

        public bool IsEditable
        {
            get { return (bool)GetValue(IsEditableProperty); }
            set { SetValue(IsEditableProperty, value); }
        }

        public bool AllowDrag
        {
            get { return (bool)GetValue(AllowDragProperty); }
            set
            {
                SetValue(AllowDragProperty, value);
            }
        }

        public bool IsBeingDragged
        {
            get { return (bool)GetValue(IsBeingDraggedProperty); }
            set
            {
                SetValue(IsBeingDraggedProperty, value);
            }
        }

        public bool HasVisibleItems
        {
            get { return (bool)GetValue(HasVisibleItemsProperty); }
            set
            {
                SetValue(HasVisibleItemsProperty, value);
            }
        }

        public bool IsFocusSelected
        {
            get { return (bool)GetValue(IsFocusSelectedProperty); }
            set { SetValue(IsFocusSelectedProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IsFocusSelected.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsFocusSelectedProperty =
            DependencyProperty.Register("IsFocusSelected", typeof(bool), 
            typeof(AsyncWpfTreeViewItem), new PropertyMetadata(false));

        

        #endregion

        protected override void OnGotKeyboardFocus(KeyboardFocusChangedEventArgs e)
        {
            base.OnGotKeyboardFocus(e);
            IsFocusSelected = true;
        }

        protected override void OnLostKeyboardFocus(KeyboardFocusChangedEventArgs e)
        {
            base.OnLostKeyboardFocus(e);
            IsFocusSelected = false;
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            base.OnKeyUp(e);
            if (e.Key == Key.Enter)
            {
                if (!Items.IsEmpty)
                {
                    IsExpanded = !IsExpanded;
                }
                OnEnterKeyPressed();
            }
            e.Handled = true;
        }

        private void OnEnterKeyPressed()
        {
            var viewModel = DataContext as ITreeViewNode<ITreeViewNodeValue>;
            if (viewModel == null) return;
            viewModel.EnterKeyCommand.Execute(this);
        }

        protected override DependencyObject GetContainerForItemOverride()
        {
            return OwnerTree.CreateTreeViewItem();
        }

        protected override bool IsItemItsOwnContainerOverride(object item)
        {
            return item is AsyncWpfTreeViewItem;
        }

        #region Drag n Drop

        #region Fields

        private bool mIsDragginOver;
        private DateTimeOffset mInitialDragOverTime;
        private readonly TimeSpan mWaitDragOverTime = TimeSpan.FromSeconds(1);
        private DraggedAdorner mDraggedAdorner;

        #endregion


        #region DragDropTemplate

        public static DragTemplateSelector GetDragDropTemplate(DependencyObject obj)
        {
            return (DragTemplateSelector)obj.GetValue(DragDropTemplateProperty);
        }

        public static void SetDragDropTemplate(DependencyObject obj, DataTemplate value)
        {
            obj.SetValue(DragDropTemplateProperty, value);
        }

        public static readonly DependencyProperty DragDropTemplateProperty =
            DependencyProperty.RegisterAttached("DragDropTemplate", typeof (DragTemplateSelector),
                typeof (AsyncWpfTreeViewItem), new UIPropertyMetadata(null));

        #endregion

        private bool isBeginDrag;

        private ICommand mExpanderClick;

        private static AsyncWpfTreeViewItem smIsMouseDownInstance;
        private static AsyncWpfTreeViewItem smIsClickedBefore;
        private static DateTime smLastClickDateTime;
        private static AsyncWpfTreeViewItem smLazyEditingItem;

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            if (smIsMouseDownInstance != this)
            {
                smIsClickedBefore = null;
                smIsMouseDownInstance = this;
                smLastClickDateTime = DateTime.Now.AddYears(1);
            }
            base.OnMouseLeftButtonDown(e);
            isBeginDrag = AllowDrag;
            e.Handled = true;
        }

        private static async void DoLazyEdit(AsyncWpfTreeViewItem lazyEditingItem)
        {
            if (lazyEditingItem == null) return;
            smLazyEditingItem = lazyEditingItem;
            await Task.Delay(100);
            var item = smLazyEditingItem;
            if (item != null)
            {
                item.IsEditing = true;
            }
        }
        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            //set node as IsEditing if it is selected and it is the only item selected.
            if (IsSelected && (OwnerTree.SelectedItems != null && OwnerTree.SelectedItems.Count == 1))
            {
                if (smIsMouseDownInstance == this && e.LeftButton == MouseButtonState.Released)
                {
                    if (smIsClickedBefore == this && DateTime.Now - smLastClickDateTime >= TimeSpan.FromMilliseconds(400))
                    {
                        DoLazyEdit(this);
                    }
                    smIsClickedBefore = this;
                    smLastClickDateTime = DateTime.Now;
                }
            }

            base.OnMouseUp(e);
        }

        protected void OwnerTreePreviewMouseMove(object sender, MouseEventArgs e)
        {
            if(isBeginDrag && e.LeftButton == MouseButtonState.Pressed)
            {
                OwnerTree.DoDragDrop(this);
                isBeginDrag = false;
            }
        }

        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            isBeginDrag = false;
            base.OnMouseLeftButtonUp(e);
            OwnerTree.EndDragDrop();
            e.Handled = false;
        }
        
        protected override void OnDragEnter(DragEventArgs e)
        {
            base.OnDragEnter(e);
            var node = DataContext as ITreeViewNode<ITreeViewNodeValue>;
            if (node != null && node.IsParentNode)
            {
                if (OwnerTree.CanDrop(e.Data, this))
                {
                    ShowDraggedAdorner(this, e.GetPosition(this), node, e.KeyStates);
                }
                else
                {
                    e.Effects = DragDropEffects.None;
                    RemoveDraggedAdorner();
                }
            }
            e.Handled = true;
        }

        protected override void OnDragOver(DragEventArgs e)
        {
            base.OnDragOver(e);
            var node = DataContext as ITreeViewNode<ITreeViewNodeValue>;
            if (node != null)
            {
                var targetNode = node.IsParentNode ? node : node.Parent;
                if (OwnerTree.CanDrop(e.Data, this))
                {
                    ShowDraggedAdorner(this, e.GetPosition(this), targetNode, e.KeyStates);
                }
                else
                {
                    e.Effects = DragDropEffects.None;
                    RemoveDraggedAdorner();
                }
            }

            if (mIsDragginOver)
            {
                if (DateTime.UtcNow - mInitialDragOverTime > mWaitDragOverTime)
                {
                    if (node != null && node.IsParentNode)
                    {
                        IsExpanded = true;
                    }
                }
            }
            else
            {
                mIsDragginOver = true;
                mInitialDragOverTime = DateTime.UtcNow;
            }
            e.Handled = true;
        }

        protected override void OnDragLeave(DragEventArgs e)
        {
            base.OnDragLeave(e);
            RemoveDraggedAdorner();
            mIsDragginOver = false;
            e.Handled = true;
        }

        protected override void OnMouseDoubleClick(MouseButtonEventArgs e)
        {
            if(IsSelected)
            {
                smLazyEditingItem = null;
                base.OnMouseDoubleClick(e);
                IsExpanded = !IsExpanded;
                var viewModel = DataContext as ITreeViewNode<ITreeViewNodeValue>;
                if (viewModel != null)
                {
                    viewModel.DoubleClickCommand.Execute(this);
                }
            }
            e.Handled = true;
        }

        

        protected override void OnDrop(DragEventArgs e)
        {
            base.OnDrop(e);
            RemoveDraggedAdorner();
            var targetItem = this;
            try
            {
                var targetDropNode = targetItem.DataContext as ITreeViewNode<ITreeViewNodeValue>;
                if (targetDropNode == null) return;
                var draggedSelection = e.Data;

                if (draggedSelection != null && OwnerTree.CanDrop(draggedSelection, this))
                {
                    bool isCtrl = (e.KeyStates & DragDropKeyStates.ControlKey) == DragDropKeyStates.ControlKey;
                    var mode = isCtrl ? DragDropEffects.Copy : DragDropEffects.Move;
                    var data = new DroppedData(DataContext, draggedSelection, mode);
                    OwnerTree.DoDrop(data);
                }
            }
            finally
            {
                OwnerTree.EndDragDrop();
                e.Handled = true;
            }
        }

        #region Drag Utils

        private static bool IsMovementBigEnough(Point initialMousePosition, Point currentPosition)
        {
            return (Math.Abs(currentPosition.X - initialMousePosition.X) >=
                    SystemParameters.MinimumHorizontalDragDistance ||
                    Math.Abs(currentPosition.Y - initialMousePosition.Y) >= SystemParameters.MinimumVerticalDragDistance);
        }

        private void ShowDraggedAdorner(AsyncWpfTreeViewItem item, Point currentPosition,
            ITreeViewNode<ITreeViewNodeValue> draggedNode, DragDropKeyStates keyStates)
        {
            if (mDraggedAdorner == null)
            {
                var adornerLayer = AdornerLayer.GetAdornerLayer(item.OwnerTree);
                mDraggedAdorner = new DraggedAdorner(draggedNode.Value, keyStates, GetDragDropTemplate(item), item, adornerLayer);
            }
            mDraggedAdorner.SetPosition(currentPosition.X, currentPosition.Y);
        }

        private void RemoveDraggedAdorner()
        {
            if (mDraggedAdorner != null)
            {
                mDraggedAdorner.Detach();
                mDraggedAdorner = null;
            }
        }

        #endregion

        #endregion
    }
}
