using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using WpfMultiselectTreeViewKit.Interfaces;
using WpfMultiselectTreeViewKit.Utils;
using Prism.Commands;
using Prism.Mvvm;

namespace WpfMultiselectTreeViewKit.ViewModels
{
    /// <summary>
    /// Base class representing the asynchronous view model for an for a Tree View item
    /// </summary>
    public abstract class TreeViewItemViewModel<TItemValueType> 
        : BindableBase,
        ITreeViewNode<TItemValueType>,
        ITreeViewUpdatableNode<TItemValueType>
        where TItemValueType : ITreeViewNodeValue, IComparable
    {
        private bool mIsEditing;
        private bool mIsEditable = true;
        #region Fields

        private readonly SortedObservableTreeItemCollection<ITreeViewNode<TItemValueType>> mItems;
        private bool mIsExpanded;
        private TItemValueType mValue;
        private bool mIsProxyNode;
        private bool mIsParentNode;
        private bool mIsSelected;
        private ITreeViewNode<TItemValueType> mParent;
        private ITreeViewViewModel<TItemValueType> mOwnerTreeView;
        
        #endregion

        #region Events


        public event ExpandCollapseEventHandler Expanding;
        public event ExpandCollapseEventHandler ExpandCompleted;
        public event ExpandCollapseEventHandler ExpandAllCompleted;
        public event ExpandCollapseEventHandler Collapsing;
        public event ExpandCollapseEventHandler CollapseCompleted;
        public event LoadAllChildrenEventHandler LoadAllChildrenCompleted;
        public event ItemsCollectionChangedEventHandler ItemsCollectionChanged;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructor
        /// </summary>
        /// <remarks>
        /// Initializes the Children collection and assigns the value for the readonly properties.
        /// </remarks>
        protected TreeViewItemViewModel(TItemValueType value, ITreeViewViewModel<TItemValueType> ownerTree,
            ITreeViewNode<TItemValueType> parentNode) : this(ownerTree, parentNode)
        {
            Value = value;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <remarks>
        /// Initializes the Children collection and assigns the value for the readonly properties.
        /// </remarks>
        protected TreeViewItemViewModel(ITreeViewViewModel<TItemValueType> ownerTreeView,
            ITreeViewNode<TItemValueType> parentNode)
        {
            mItems = new SortedObservableTreeItemCollection<ITreeViewNode<TItemValueType>>();
            mItems.CollectionChanged += OnItemsCollectionChanged;
            OwnerTreeView = ownerTreeView;
            Parent = parentNode;
            EnterKeyCommand = new DelegateCommand(OnEnteryKeyPressed);
        }
        
        #endregion

        /// <summary>
        /// Handles the event of the <see cref="Items"/> observable collection changing.
        /// </summary>
        /// <remarks>
        /// Raises the <see cref="ItemsCollectionChanged"/> event to notify that the Items collection has changed.
        /// <para>
        /// Since the output type of the <see cref="Items"/> collection is not more than an IEnumerable, 
        /// an extra event is required to notify external objects without making the Items collection publicly updatable.
        /// </para>
        /// </remarks>
        /// <param name="sender">The collection that has changed; in this case, <see cref="mItems"/>.</param>
        /// <param name="e">The event arguments.</param>
        private void OnItemsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            OnItemsCollectionChanged();
            var handler = ItemsCollectionChanged;
            if (handler == null) return;
            System.Windows.Application.Current.Dispatcher.Invoke(() => handler(this));
        }

        protected virtual void OnItemsCollectionChanged()
        {
            HasVisibleItems = mItems.Any();
        }

        public virtual bool IsItemContainerNode { get { return IsParentNode || IsProxyNode; } }

        #region Properties

        /// <summary>
        /// Gets or sets the original Value handled by this item
        /// </summary>
        public TItemValueType Value
        {
            get { return mValue; }
            protected set { SetProperty(ref mValue, value); }
        }

        /// <summary>
        /// Gets or sets the reference to the owner TreeViewViewModel
        /// </summary>
        public object OwnerTreeView
        {
            get { return mOwnerTreeView; }
            protected set
            {
                var treeView = value as ITreeViewViewModel<TItemValueType>;
                if (treeView == null) return;
                SetProperty(ref mOwnerTreeView, treeView);
            }
        }

        /// <summary>
        /// Gets wether this node is root.
        /// </summary>
        /// <remarks>
        /// If not parent has been defined, this node is considered as root.
        /// </remarks>
        public bool IsRootNode
        {
            get { return Parent == null; }
        }

        /// <summary>
        /// Gets or sets the parent node.
        /// </summary>
        public ITreeViewNode<TItemValueType> Parent
        {
            get { return mParent; }
            protected set { SetProperty(ref mParent, value); }
        }

        /// <summary>
        /// Gets or sets the sub items
        /// </summary>
        public virtual IEnumerable<ITreeViewNode<TItemValueType>> Items
        {
            get { return mItems; }
        }

        /// <summary>
        /// Gets or sets wether the item is in an Expanded state
        /// </summary>
        public bool IsExpanded
        {
            get { return mIsExpanded; }
            set
            {
                if (IsExpanded == value) return;
                mIsExpanded = value;
                OnIsExpandedChanged();
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets wether the node is being edited.
        /// </summary>
        public bool IsEditing
        {
            get { return mIsEditing; }
            set { SetProperty(ref mIsEditing, value && IsEditable); }
        }

        /// <summary>
        /// Gets or sets wether the node is editable.
        /// </summary>
        public virtual bool IsEditable
        {
            get { return mIsEditable; }
            set { SetProperty(ref mIsEditable, value); }
        }

        /// <summary>
        /// Gets or sets wether the item is Selected
        /// </summary>
        public bool IsSelected
        {
            get { return mIsSelected; }
            set
            {
                if (mIsSelected == value) return;
                mIsSelected = value;
                if (mIsSelected)
                {
                    ExecuteOnSelected();
                }
                OnPropertyChanged();
            }
        }

        public virtual bool IsMultiSelected
        {
            get { return mIsMultiSelected; }
            set { SetProperty(ref mIsMultiSelected, value); }
        }

        public bool HasVisibleItems
        {
            get { return mHasVisibleItems; }
            set { SetProperty(ref mHasVisibleItems, value); }
        }

        /// <summary>
        /// Gets or sets wether loaded children should be cleared if the item gets collapsed
        /// </summary>
        public bool ShouldUnloadChildrenOnCollapse
        {
            get { return mOwnerTreeView.ShouldUnloadChildrenOnCollapse; }
        }

        /// <summary>
        /// Gets ir sets wether this item should be treated as parentNode
        /// </summary>
        public virtual bool IsParentNode
        {
            get { return mIsParentNode; }
            set { SetProperty(ref mIsParentNode, value); }
        }

        /// <summary>
        /// Gets wether this item is a leaf node.
        /// </summary>
        public virtual bool IsLeaf
        {
            get { return !IsParentNode && !Items.Any(); }
        }

        /// <summary>
        /// Gets wether the Children are loaded.
        /// </summary>
        public bool IsChildrenLoaded
        {
            //stating that children are loaded means that there is no proxy child node
            get { return IsParentNode && Items.Any() && !Items.Any(item => item.IsProxyNode); }
        }

        /// <summary>
        /// Gets wether this item is a proxy node, used as a placeholder for children items.
        /// </summary>
        public virtual bool IsProxyNode
        {
            get { return mIsProxyNode; }
            set { SetProperty(ref mIsProxyNode, value); }
        }

        #endregion
        
        #region Asynchronous Method References for children data loading

        /// <summary>
        /// Gets or sets the reference for the asynchronous method that would handle
        /// the event to load the node's children without expanding or selecting.
        /// </summary>
        public ForceLoadChildrenEventHandler OnForceLoadChildren { get; set; }

        /// <summary>
        /// Gets or sets the reference for the asynchronous method that would handle
        /// the Expanded event
        /// </summary>
        public ExpandCollapseAsyncEventHandler OnExpanded { get; set; }

        /// <summary>
        /// Gets or sets the reference for the asychronous method that would handle
        /// the Collapsed event.
        /// </summary>
        public ExpandCollapseEventHandler OnCollapsed { get; set; }

        /// <summary>
        /// Gets or sets the reference for the asychronous method that would handle
        /// the Selected event.
        /// </summary>
        public SelectedItemAsyncEventHandler OnSelected { get; set; }

        #endregion
        
        #region Private Methods - Async operations for Expand/Collapse

        /// <summary>
        /// Sets the IsExpanded backend property to the specified value and calls <see cref="OnIsExpandedChanged"/>.
        /// </summary>
        /// <param name="newValue">The new value for the IsExpanded property.</param>
        /// <param name="raiseEvent">Indicates if the events for Exand/Collapse should be raised. True by default.</param>
        private void SetIsExpanded(bool newValue, bool raiseEvent = true)
        {
            SetProperty(ref mIsExpanded, newValue, PropertySupport.ExtractPropertyName(() => IsExpanded));
            if(raiseEvent)
            {
                OnIsExpandedChanged();
            }
        }

        /// <summary>
        /// Sets the IsSelected backend property to the specified value and calls <see cref="ExecuteOnSelected"/>.
        /// </summary>
        /// <param name="newValue">The new value for the IsExpanded property.</param>
        /// <param name="raiseEvent">Indicates if the events for Selecting should be raised. True by default.</param>
        private void SetIsSelected(bool newValue, bool raiseEvent = true)
        {
            SetProperty(ref mIsSelected, newValue, PropertySupport.ExtractPropertyName(() => IsSelected));
            if (raiseEvent)
            {
                ExecuteOnSelected();
            }
        }

        /// <summary>
        /// Execute the handler for OnExpanded
        /// </summary>
        /// <remarks>Uses null reference validation and logs exceptions.</remarks>
        private async void ExecuteOnExpanded()
        {
            try
            {
                OnExpandCollapseEvent(Expanding);
                if(!IsChildrenLoaded)
                {
                    await OnExpanded(this);
                }
                OnExpandCollapseEvent(ExpandCompleted);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private void OnIsExpandedChanged()
        {
            if (IsExpanded)
            {
                ExecuteOnExpanded();
            }
            else
            {
                ExecuteOnCollapsed();
            }
        }

        /// <summary>
        /// Execute the handler for OnCollapsed
        /// </summary>
        /// <remarks>Uses null reference validation and logs exceptions.</remarks>
        private void ExecuteOnCollapsed()
        {
            if (!ShouldUnloadChildrenOnCollapse) return;
            try
            {
                OnExpandCollapseEvent(Collapsing);
                OnCollapsed(this);
                OnExpandCollapseEvent(CollapseCompleted);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        /// <summary>
        /// Execute the handler for OnSelected
        /// </summary>
        /// <remarks>Uses null reference validation and logs exceptions.</remarks>
        private async void ExecuteOnSelected()
        {
            try
            {
                var handler = OnSelected;
                if (handler == null || IsChildrenLoaded) return;
                await handler(this);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        #endregion

        #region Public Silent Methods

        public void SetSilentSelect(bool isSelected)
        {
            SetIsSelected(isSelected, false);
        }

        public void SetSilentExpand(bool isExpanded)
        {
            SetIsExpanded(isExpanded, false);
        }

        #endregion


        #region Expand/Collapse methods

        public async void ExpandSelect()
        {
            await LoadChildren(false, false, true, false);
            //SetIsExpanded(true);
            SetIsSelected(true, false);
        }

        ///// <summary>
        ///// Expands the item.
        ///// </summary>
        //public void Expand()
        //{
        //    SetIsExpanded(true);
        //}

        /// <summary>
        /// Expands the item and all sub items.
        /// </summary>
        public void ExpandAll(bool forceReload = false)
        {
            OnExpandAll(forceReload);
        }

        private async void OnExpandAll(bool forceReload)
        {
            await LoadChildren(forceReload, deepLoadChildren: true, expandAfterLoad: true);
            OnExpandCollapseEvent(ExpandAllCompleted);
        }

        /// <summary>
        /// Collapses the item and all sub items.
        /// </summary>
        public void CollapseAll()
        {
            if (!ShouldUnloadChildrenOnCollapse)
            {
                foreach (var item in Items)
                {
                    item.CollapseAll();
                }
            }
            SetIsExpanded(false, ShouldUnloadChildrenOnCollapse);
        }

        /// <summary>
        /// Loads the children of all nodes without expanding the parents.
        /// </summary>
        public void LoadAll()
        {
            OnLoadAll();
        }

        public Task LoadAllAsync()
        {
            return OnLoadAllAsync();
        }

        private async void OnLoadAll()
        {
            await OnLoadAllAsync();
        }

        private async Task OnLoadAllAsync()
        {
            //loads all nodes and maintains current expand status
            await LoadChildren(forceIfAlreadyLoaded: true, deepLoadChildren: true);
            RaiseOnLoadAllCompleted();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Sets the value for the Items property
        /// </summary>
        /// <param name="items">The new collection.</param>
        /// <param name="resetCollection">If true, indicates that the entire Items collection will be reset 
        /// with the contents of the items parameter; otherwise, indicates that the Items collection must be
        /// updated with the differences from the items parameter (add new, remove non-existent). </param>
        public void SetItems(IEnumerable<ITreeViewNode<TItemValueType>> items, bool resetCollection = true)
        {
            mItems.InitializeWith(items);
            return;
            if (resetCollection)
            {
                mItems.InitializeWith(items);
            }
            else
            {
                RemoveProxyNodes();
                if (mItems.Any())
                {
                    mItems.CombineWith(items);
                }
                else
                {
                    mItems.AddRange(items);
                }
            }
            if (!mItems.Any())
            {
                SetIsExpanded(false, false);
            }
            
        }

        /// <summary>
        /// Sets the value for the Items property
        /// </summary>
        /// <param name="item">The new collection.</param>
        public void SetItems(ITreeViewNode<TItemValueType> item)
        {
            if(item!=null)
            {
                mItems.InitializeWith(item);
            }
            if (!mItems.Any())
            {
                SetSilentExpand(false);
            }
        }

        private void RemoveProxyNodes()
        {
            var proxyNodes = mItems.Where(item => item.IsProxyNode).ToList();
            mItems.RemoveRange(proxyNodes);
        }

        private bool mIsLoadingChildren;
        private bool mIsBeingDragged;
        private bool mHasVisibleItems;
        private bool mIsMultiSelected;
        private ICommand mDoubleClickCommand;
        private ICommand mClickCommand;

        /// <summary>
        /// Forcibly executes the event to load children
        /// </summary>
        public async Task LoadChildren(bool forceIfAlreadyLoaded = false, bool deepLoadChildren = false, 
            bool expandAfterLoad = false, bool maintainCurrentExpandStatus = true)
        {
            try
            {
                if (mIsLoadingChildren) return;
                mIsLoadingChildren = forceIfAlreadyLoaded;
                bool currentExpandStatus = IsExpanded;
                var handler = OnForceLoadChildren;
                if (handler != null)
                {
                    await handler(this, forceIfAlreadyLoaded);                    
                }
                if(mItems.Any())
                {
                    if (deepLoadChildren)
                    {
                        await DeepLoadAllChildren(forceIfAlreadyLoaded, expandAfterLoad);
                    }
                    SetSilentExpand(expandAfterLoad || (maintainCurrentExpandStatus && currentExpandStatus));
                }
                mIsLoadingChildren = false;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private async Task DeepLoadAllChildren(bool forceIfAlreadyLoaded = false, bool expandAfterLoad = false, bool maintainCurrentExpandStatus = true)
        {
            if (Items == null) return;
            foreach (var child in Items)
            {
                await child.LoadChildren(forceIfAlreadyLoaded: forceIfAlreadyLoaded, deepLoadChildren: true, expandAfterLoad: expandAfterLoad, maintainCurrentExpandStatus: maintainCurrentExpandStatus);
            }
        }

        #endregion


        #region Event callers

        private void OnExpandCollapseEvent(ExpandCollapseEventHandler handler)
        {
            if (handler == null) return;
            handler(this);
        }

        private void RaiseOnLoadAllCompleted()
        {
            var handler = LoadAllChildrenCompleted;
            if (handler == null) return;
            handler(this);
        }

        #endregion

        /// <summary>
        /// Compares this item to another node for sorting purposes.
        /// </summary>
        /// <param name="other">The node to be compared against</param>
        public abstract int CompareTo(object other);

        /// <summary>
        /// Inserts an item to the <see cref="Items"/> collection.
        /// </summary>
        /// <param name="newChild">The new item.</param>
        /// <remarks>
        /// By default, validates if it does not already contain <see cref="newChild"/>
        /// Any Proxy node existing in the <see cref="Items"/> collection will be removed 
        /// before adding the new node.
        /// </remarks>
        public void InsertChild(ITreeViewNode<TItemValueType> newChild)
        {
            if (mItems.Contains(newChild)) return;
            //remove all proxy nodes
            foreach (var node in mItems.Where(node => node.IsProxyNode))
            {
                mItems.Remove(node);
            }
            mItems.Add(newChild);
        }

        /// <summary>
        /// Removes an item from the <see cref="Items"/> collection.
        /// </summary>
        /// <param name="child">The item to be removed.</param>
        /// <remarks>
        /// Internally validates if the node exists within the <see cref="Items"/> collection.
        /// </remarks>
        public void RemoveChild(ITreeViewNode<TItemValueType> child)
        {
            if(mItems.Contains(child))
            {
                mItems.Remove(child);
            }
        }

        /// <summary>
        /// Ensures that the item is visible by forcing the ancestors to expand
        /// </summary>
        public async void EnsureVisibleSelected()
        {
            var parent = Parent;
            while (parent != null)
            {
                if (!parent.IsChildrenLoaded)
                {
                    await parent.LoadChildren();                    
                }
                if(!parent.IsExpanded)
                {
                    parent.SetSilentExpand(true);
                }
                parent = parent.Parent;
            }
            IsSelected = true;
        }

        public ICommand EnterKeyCommand { get; private set; }

        protected abstract void OnEnteryKeyPressed();

        public virtual void RefreshData()
        {
            OnPropertyChanged(PropertySupport.ExtractPropertyName(()=>Value));
        }

        public virtual void Refresh()
        {
        }

        public bool IsBeingDragged
        {
            get { return mIsBeingDragged; }
            set { SetProperty(ref mIsBeingDragged, value); }
        }

        #region Utils

        public IEnumerable<ITreeViewNode<TItemValueType>> GetAllNodes()
        {
            return Items.SelectMany(GetAllNodes);
        }

        private IEnumerable<ITreeViewNode<TItemValueType>> GetAllNodes(ITreeViewNode<TItemValueType> node)
        {
            yield return node;
            foreach (var childNode in node.Items.SelectMany(GetAllNodes))
            {
                yield return childNode;
            }
        }

        #endregion

        public ICommand DoubleClickCommand
        {
            get { return mDoubleClickCommand ?? (mDoubleClickCommand = new DelegateCommand(OnDoubleClick)); }
        }

        protected abstract void OnDoubleClick();
    }
}