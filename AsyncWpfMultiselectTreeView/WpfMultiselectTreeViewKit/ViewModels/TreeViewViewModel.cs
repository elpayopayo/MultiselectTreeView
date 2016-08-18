using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using WpfMultiselectTreeViewKit.Interfaces;
using WpfMultiselectTreeViewKit.UserControl;
using WpfMultiselectTreeViewKit.Utils;
using Prism.Mvvm;

namespace WpfMultiselectTreeViewKit.ViewModels
{
    /// <summary>
    /// Base class representing the asynchronous View Model for a TreeView
    /// </summary>
    public abstract class TreeViewViewModel<TDataSourceType, TItemValueType> 
        : BindableBase, 
        ITreeViewViewModel<TItemValueType>,
        IInitializableTreeView<TDataSourceType>
        where TItemValueType : ITreeViewNodeValue, IComparable
    {
        #region Fields

        private readonly SortedObservableTreeItemCollection<ITreeViewNode<TItemValueType>> mItems;
        private ITreeViewNode<TItemValueType> mLastSelectedItem;
        private bool mLoadChildrenOnSelected;

        private readonly ConcurrentDictionary<object, CancellationTokenSource> mExpandCancellationTokenSources = new ConcurrentDictionary<object, CancellationTokenSource>();
        private readonly ConcurrentDictionary<object, CancellationTokenSource> mCollapseCancellationTokenSources = new ConcurrentDictionary<object, CancellationTokenSource>();
        private readonly ConcurrentDictionary<object, SemaphoreSlim> mLoadChildrenSemaphores = new ConcurrentDictionary<object, SemaphoreSlim>();
        private IList mNonTypedSelectedItems;
        private IEnumerable<ITreeViewNode<TItemValueType>> mSelectedItems;
        private bool mShouldUnloadChildrenOnCollapse = false;
        private object mNonTypedLastSelectedItem;

        #endregion

        #region Events

        public event SelectedItemChangedEventHandler<TItemValueType> SelectedItemChanged;

        #endregion


        #region Constructor

        /// <summary>
        /// Initializes the TreeViewViewModel instance.
        /// </summary>
        /// <remarks>
        /// By default, initializes the <see cref="Items"/> collection.
        /// </remarks>
        protected TreeViewViewModel()
        {
            mItems = new SortedObservableTreeItemCollection<ITreeViewNode<TItemValueType>>();
        }

        #endregion


        #region Properties

        /// <summary>
        /// Gets the collection of asynchronous tree view items used by the Tree View
        /// </summary>
        public IEnumerable<ITreeViewNode<TItemValueType>> Items
        {
            get { return mItems; }
        }

        /// <summary>
        /// Gets or sets the Selected Item from the view
        /// </summary>
        /// <remarks>
        /// Besides holding the reference to the selected item, also sets the property IsSelected of
        /// that item, if not already set to true.
        /// </remarks>
        public ITreeViewNode<TItemValueType> LastSelectedItem
        {
            get { return mLastSelectedItem; }
            protected set
            {
                if (value == mLastSelectedItem) return;
                SetProperty(ref mLastSelectedItem, value);
                if (mLastSelectedItem != null && !mLastSelectedItem.IsSelected)
                {
                    mLastSelectedItem.EnsureVisibleSelected();
                }
            }
        }

        /// <summary>
        /// Gets or sets the non-typed collection of selected items.
        /// </summary>
        /// <remarks>
        /// When setting, validates that all items in the collection are of the same type; if not, the property is not set.
        /// <para>The property <see cref="SelectedItems"/> is set with the typed collection identified.</para>
        /// </remarks>
        public IList NonTypedSelectedItems
        {
            get { return mNonTypedSelectedItems; }
            set
            {
                SetProperty(ref mNonTypedSelectedItems, value);
                IList<ITreeViewNode<TItemValueType>> selectedItems = null;
                if (value != null)
                {
                    selectedItems = value.OfType<ITreeViewNode<TItemValueType>>().ToList();
                    //if not all the items are of the same type, do not assign the property
                    if (selectedItems.Count != value.Count) return;
                }
                //set the SelectedItems collection
                SelectedItems = selectedItems;
            }
        }

        /// <summary>
        /// Gets or sets the non-typed last selected item.
        /// </summary>
        /// <remarks>
        /// The property <see cref="LastSelectedItem"/> is set with the typed item.
        /// </remarks>
        public object NonTypedLastSelectedItem
        {
            get { return mNonTypedLastSelectedItem; }
            set
            {
                SetProperty(ref mNonTypedLastSelectedItem, value);
                LastSelectedItem = mNonTypedLastSelectedItem as ITreeViewNode<TItemValueType>;
            }
        }

        /// <summary>
        /// Gets or privately sets the strongly typed collection of items
        /// </summary>
        /// <remarks>
        /// Value is set through <see cref="NonTypedSelectedItems"/> setter.
        /// </remarks>
        public IEnumerable<ITreeViewNode<TItemValueType>> SelectedItems
        {
            get { return mSelectedItems; }
            private set { SetProperty(ref mSelectedItems, value); }
        }

        /// <summary>
        /// Gets or sets wether the children of selected items should be loaded
        /// </summary>
        public bool LoadChildrenOnSelected
        {
            get { return mLoadChildrenOnSelected; }
            set
            {
                SetProperty(ref mLoadChildrenOnSelected, value);
                OnLoadChildrenOnSelectedChanged();
            }
        }

        public bool ShouldUnloadChildrenOnCollapse
        {
            get { return mShouldUnloadChildrenOnCollapse; }
        }

        /// <summary>
        /// Executes the handler defined for when LoadChildrenOnSelected changes.
        /// </summary>
        /// <remarks>
        /// Internally validates if an item is already selected and forces the trigger
        /// of OnSelectedAsync for that item.
        /// </remarks>
        private async void OnLoadChildrenOnSelectedChanged()
        {
            //only execute if _loadChildrenOnSelected is true, there is on selected item and the selected item has not yet loaded its children
            if (!mLoadChildrenOnSelected || LastSelectedItem == null || LastSelectedItem.IsChildrenLoaded) return;
            try
            {
                await OnSelectedAsync(LastSelectedItem);
            }
            catch (Exception e)
            {
                //log exception
                Console.WriteLine(e);
            }
        }

        #endregion


        #region Abstract Declarations

        /// <summary>
        /// Initializes the data for the tree view
        /// </summary>
        /// <param name="dataSource">The data that will be used to initialize the <see cref="Items"/>.</param>
        public abstract void Initialize(TDataSourceType dataSource);

        /// <summary>
        /// Asynchronoysly retrieves the collection of items to be set as children for the provided instance
        /// </summary>
        /// <param name="treeViewItemViewModel">The instance for which children will be retrieved.</param>
        /// <param name="cancellationToken">A token used for cancellation of the Task.</param>
        /// <returns>An awaitable Task of <see cref="IList{IAsyncTreeViewItem}"/></returns>
        protected abstract Task<IEnumerable<ITreeViewNode<TItemValueType>>> GetChildren(ITreeViewNode<TItemValueType> treeViewItemViewModel, CancellationToken cancellationToken);


        public abstract void DoDragDrop(object dragSource);

        public abstract void DoDrop(DroppedData data);

        public virtual bool CanDrop(IDataObject source, object targetViewModel)
        {
            return true;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Removes all nodes from the tree.
        /// </summary>
        public void Clear()
        {
            NonTypedSelectedItems = null;
            SelectedItems = null;
            LastSelectedItem = null;
            mItems.Clear();
            OnPropertyChanged(PropertySupport.ExtractPropertyName(() => Items));
        }

        /// <summary>
        /// Sets the last selected item.
        /// </summary>
        public void SetLastSelectedItem(ITreeViewNode<TItemValueType> lastSelectedItem)
        {
            LastSelectedItem = lastSelectedItem;
        }

        protected void AddRootNode(ITreeViewNode<TItemValueType> rootNode)
        {
            if (rootNode == null) return;
            mItems.InitializeWith(rootNode);
            OnPropertyChanged(PropertySupport.ExtractPropertyName(()=>Items));
        }
        
        /// <summary>
        /// Creates a tree view item of the specified type and data.
        /// </summary>
        /// <typeparam name="T">The node type.</typeparam>
        /// <param name="itemValueType">The value that the node must handle.</param>
        /// <param name="parentNode">The parent for the new node.</param>
        /// <returns>The generated instance of <see cref="T"/></returns>
        protected T CreateTreeNode<T>(TItemValueType itemValueType, ITreeViewNode<TItemValueType> parentNode)
            where T : ITreeViewNode<TItemValueType>
        {
            var node = (T)Activator.CreateInstance(typeof(T), itemValueType, this, parentNode) ;

            if (node.IsParentNode)
            {
                var controllableNode = node as ITreeViewUpdatableNode<TItemValueType>;
                if (controllableNode == null)
                {
                    throw new ArgumentException("T is not of type ITreeViewControllableNode<TItemValueType>");
                }
                controllableNode.SetItems(new List<ITreeViewNode<TItemValueType>> { CreateProxyNode(this, parentNode) });
                node.OnExpanded = OnExpandedAsync;
                node.OnCollapsed = OnCollapsed;
                node.OnSelected = OnSelectedAsync;
                node.OnForceLoadChildren = OnForceLoadChildrenAsync;
            }
            return node;
        }

        /// <summary>
        /// Creates a proxy node
        /// </summary>
        /// <returns>An instance of <see cref="ProxyTreeViewItemViewModel{TValue}"/></returns>
        public abstract ProxyTreeViewItemViewModel<TItemValueType> CreateProxyNode(ITreeViewViewModel<TItemValueType> ownerTreeView, ITreeViewNode<TItemValueType> parentNode);

        /// <summary>
        /// Asynchronous task that fills the children collection of the provided asyncTreeViewItem.
        /// </summary>
        /// <param name="item">The item for which the children collection will be set.</param>
        /// <param name="cancellationToken">A token used to cancel the Task.</param>
        /// <param name="forceIfAlreadyLoaded"></param>
        /// <returns>An asynchronous void Task.</returns>
        private async Task FillChildren(ITreeViewNode<TItemValueType> item, CancellationToken cancellationToken, bool forceIfAlreadyLoaded = false)
        {
            var settableItem = item as ITreeViewUpdatableNode<TItemValueType>;
            if (settableItem == null) return;
            /*
             * since both Expand and Select events can call FillChildren for the same item, 
             * a semaphore was required to control concurrent execution
             * so that only one of the callers get to fill the children of the tree view item.
             * 
             * A different semaphore is stored in a dictionary where the item is the key.
             * */
            var semaphore = mLoadChildrenSemaphores.GetOrAdd(item, new SemaphoreSlim(1, 1));
            await semaphore.WaitAsync(cancellationToken);

            try
            {
                //if the children have already been loaded, there's no need to re-load them
                //unless, a forced reload is requested
                if (item.IsChildrenLoaded && !forceIfAlreadyLoaded) return;
                
                //if the cancellation has been requested at this point, end the operation
                if (cancellationToken.IsCancellationRequested) return;
                //retrieve the collection of nodes that should be set as children for the current item
                var childItems = await GetChildren(item, cancellationToken);
                //if the cancellation has been requested at this point, end the operation
                if (cancellationToken.IsCancellationRequested) return;
                //assign the retrieved collection to the Items property
                //if this property is bound, the UI will begin displaying items at this point
                if(childItems!=null)
                {
                    settableItem.SetItems(childItems.Where(childItem => childItem != null));
                }
            }
            finally
            {
                //Regardless of whatever happens, release the semaphore to avoid deadlocks
                semaphore.Release();
            }
        }

        /// <summary>
        /// Asynchronous operation executed when an item gets expanded.
        /// </summary>
        /// <remarks>
        /// Internally calls <see cref="OnLoadChildren"/>
        /// </remarks>
        /// <param name="sender">The item that got expanded.</param>
        /// <returns>An asynchronous task.</returns>
        private async Task OnExpandedAsync(object sender)
        {
            var item = sender as ITreeViewNode<TItemValueType>;
            if (item == null) return;
            await OnLoadChildren(item);
        }

        /// <summary>
        /// Asynchronous operation executed when an item requests to forcibly load its children.
        /// </summary>
        /// <remarks>
        /// Internally calls <see cref="OnLoadChildren"/>
        /// </remarks>
        /// <param name="sender">The item that requested the forced load of children.</param>
        /// <param name="forceIfAlreadyLoaded">Indicates if the loading of children should be applied even if they were loaded previously.</param>
        /// <returns>An asynchronous task.</returns>
        private async Task OnForceLoadChildrenAsync(object sender, bool forceIfAlreadyLoaded = false)
        {
            var item = sender as ITreeViewNode<TItemValueType>;
            if (item == null) return;
            await OnLoadChildren(item, forceIfAlreadyLoaded);
        }

        /// <summary>
        /// Cancels an expand operation for the provided item
        /// </summary>
        /// <param name="treeViewItemViewModel">The item for which the expand will be cancelled.</param>
        private void CancelExpand(ITreeViewNode<TItemValueType> treeViewItemViewModel)
        {
            CancellationTokenSource cancellationtokenSource;
            if (mExpandCancellationTokenSources.TryGetValue(treeViewItemViewModel, out cancellationtokenSource) &&
                cancellationtokenSource != null)
            {
                cancellationtokenSource.Cancel();
            }
        }

        /// <summary>
        /// Asynchronous operation executed when an item gets collapsed.
        /// </summary>
        /// <remarks>
        /// Internally executes the corresponding operations that would set a proxy node into the Chidren property of the item.
        /// </remarks>
        /// <param name="sender">The item that got collapsed.</param>
        private void OnCollapsed(object sender)
        {
            var item = sender as ITreeViewNode<TItemValueType>;
            if (item == null) return;

            //cancel an existing expand operation for the item
            CancelExpand(item);

            //get a cancellation token for the node collapse
            var cancellationTokenSource = mCollapseCancellationTokenSources.GetOrAdd(item, new CancellationTokenSource());
            var token = cancellationTokenSource.Token;
            try
            {
                //validate against operation cancellation
                if (token.IsCancellationRequested) return;

                //validate against properties related to the collapse behavior
                if (!ShouldUnloadChildrenOnCollapse || !item.IsParentNode ||
                    (LoadChildrenOnSelected && item.IsSelected))
                    return;

                var settableItem = item as ITreeViewUpdatableNode<TItemValueType>;
                if (settableItem == null) return;
                //assign a list with a proxy node to the Children property for the item
                settableItem.SetItems(CreateProxyNode(this, item));
            }
            finally
            {
                //regardles of the result, remove the cancellation token
                mCollapseCancellationTokenSources.TryRemove(item, out cancellationTokenSource);
            }
        }

        /// <summary>
        /// Cancell a collapse operation for the item.
        /// </summary>
        /// <param name="treeViewItemViewModel">The item for which the collapse operation will be cancelled.</param>
        private void CancelCollapse(ITreeViewNode<TItemValueType> treeViewItemViewModel)
        {
            CancellationTokenSource cancellationTokenSource;
            if (mCollapseCancellationTokenSources.TryGetValue(treeViewItemViewModel, out cancellationTokenSource) &&
                cancellationTokenSource != null)
            {
                cancellationTokenSource.Cancel();
            }
        }

        /// <summary>
        /// Asynchronous operation executed when an item gets selected.
        /// </summary>
        /// <remarks>
        /// Internally validates if Children should be loaded when the item gets selected
        /// and calls OnLoadChildren
        /// </remarks>
        /// <param name="sender">The item that got expanded.</param>
        /// <returns>An asynchronous task.</returns>
        private async Task OnSelectedAsync(object sender)
        {
            var item = sender as ITreeViewNode<TItemValueType>;
            if (item == null) return;
            if (LoadChildrenOnSelected)
            {
                await OnLoadChildren(item);
            }
            RaiseOnSelectedItemChanged(item);
        }

        private void RaiseOnSelectedItemChanged(ITreeViewNode<TItemValueType> selectedItem)
        {
            var handler = SelectedItemChanged;
            if (handler == null) return;
            handler(selectedItem);
        }

        /// <summary>
        /// Asynchronous operation executed when Children of an item should be loaded
        /// </summary>
        /// <remarks>
        /// Internally executes the corresponding operations that would fill the Children property of the item.
        /// </remarks>
        /// <param name="treeViewItemViewModel">The item that got expanded.</param>
        /// <param name="forceIfAlreadyLoaded"></param>
        /// <returns>An asynchronous task.</returns>
        private async Task OnLoadChildren(ITreeViewNode<TItemValueType> treeViewItemViewModel, bool forceIfAlreadyLoaded = false)
        {
            if (treeViewItemViewModel == null) return;

            //if an expand is triggered, then cancel the Collapse operation for this item
            CancelCollapse(treeViewItemViewModel);

            //get a cancellation token for the node expansion
            var cancellationTokenSource = mExpandCancellationTokenSources.GetOrAdd(treeViewItemViewModel, new CancellationTokenSource());

            try
            {
                //execute the asynchronous operation that fills the Children property for the item
                await FillChildren(treeViewItemViewModel, cancellationTokenSource.Token, forceIfAlreadyLoaded);
            }
            finally
            {
                //regardless of the result, remove the token when the operation completes
                mExpandCancellationTokenSources.TryRemove(treeViewItemViewModel, out cancellationTokenSource);
            }
        }

        /// <summary>
        /// Executes the 
        /// </summary>
        /// <param name="treeViewItem"></param>
        /// <returns></returns>
        public async Task LoadChildren(ITreeViewNode<TItemValueType> treeViewItem)
        {
            await OnLoadChildren(treeViewItem);
        }

        /// <summary>
        /// Inserts an child item to the corresponding parent node.
        /// </summary>
        /// <param name="newItem">The new item to be insterted.</param>
        /// <remarks>
        /// If the <see cref="newItem"/> does not have a parent reference, 
        /// it will be inserted directly into the <see cref="Items"/> collection.
        /// <para>
        /// If the <see cref="newItem"/> does have a parent reference, 
        /// it will be inserted as a child of that parent node if not already contained.
        /// </para>
        /// </remarks>
        protected void InsertItem(ITreeViewNode<TItemValueType> newItem)
        {
            if (newItem == null) return;
            if (newItem.Parent == null)
            {
                mItems.Add(newItem);
            }
            else
            {
                var parent = newItem.Parent;
                var controllableParent = parent as ITreeViewUpdatableNode<TItemValueType>;
                if (parent != null && controllableParent != null)
                {
                    controllableParent.InsertChild(newItem);
                }
            }
        }

        #endregion

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
    }
}