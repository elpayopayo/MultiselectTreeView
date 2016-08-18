using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Input;

namespace WpfMultiselectTreeViewKit.Interfaces
{

    #region Delegate Definitions

    /// <summary>
    /// Delegate for the LoadAllChildren event
    /// </summary>
    /// <param name="sender">The TreeViewItem that triggered the event.</param>
    /// <returns>Task to be handled asynchronously.</returns>
    public delegate void LoadAllChildrenEventHandler(object sender);

    /// <summary>
    /// Delegate for the events of Expand and Collapse
    /// </summary>
    /// <param name="sender">The TreeViewItem that triggered the event.</param>
    /// <returns>Task to be handled asynchronously.</returns>
    public delegate void ExpandCollapseEventHandler(object sender);

    /// <summary>
    /// Async Delegate for the Functions of Expand and Collapse
    /// </summary>
    /// <param name="sender">The TreeViewItem that triggered the event.</param>
    /// <returns>Task to be handled asynchronously.</returns>
    public delegate Task ExpandCollapseAsyncEventHandler(object sender);

    /// <summary>
    /// Delegate for the event of Selecting an item
    /// </summary>
    /// <param name="sender">The TreeViewItem that triggered the event.</param>
    /// <returns>Task to be handled asynchronously.</returns>
    public delegate void SelectedItemEventHandler(object sender);

    /// <summary>
    /// Async Delegate for the Function of Selecting an item
    /// </summary>
    /// <param name="sender">The TreeViewItem that triggered the event.</param>
    /// <returns>Task to be handled asynchronously.</returns>
    public delegate Task SelectedItemAsyncEventHandler(object sender);

    /// <summary>
    /// Delegate for the event of forcing the loading of an item's children without expanding or selecting the item.
    /// </summary>
    /// <param name="sender">The TreeViewItem that triggered the event.</param>
    /// <param name="forceIfAlreadyLoaded">Indicates if children should be loaded even if loaded previously.</param>
    /// <returns>Task to be handled asynchronously.</returns>
    public delegate Task ForceLoadChildrenEventHandler(object sender, bool forceIfAlreadyLoaded = false);

    /// <summary>
    /// Delegate for the event of Items collection changing its contents.
    /// </summary>
    /// <param name="sender">The node whose Items collection has changed.</param>
    public delegate void ItemsCollectionChangedEventHandler(object sender);

    #endregion

    /// <summary>
    /// Interface that represents a Tree View Node
    /// </summary>
    /// <typeparam name="TItemValueType">The type of item hosted by the node.</typeparam>
    public interface ITreeViewNode<out TItemValueType> : IComparable, INotifyPropertyChanged
        where TItemValueType : ITreeViewNodeValue
    {
        event ExpandCollapseEventHandler Expanding;
        event ExpandCollapseEventHandler ExpandCompleted;
        event ExpandCollapseEventHandler ExpandAllCompleted;
        event ExpandCollapseEventHandler Collapsing;
        event ExpandCollapseEventHandler CollapseCompleted;
        event LoadAllChildrenEventHandler LoadAllChildrenCompleted;
        event ItemsCollectionChangedEventHandler ItemsCollectionChanged;

        /// <summary>
        /// Gets or sets the reference for the asynchronous method that would handle
        /// the event to forcibly load children, regardles of the value of IsExpanded.
        /// </summary>
        ForceLoadChildrenEventHandler OnForceLoadChildren { get; set; }

        /// <summary>
        /// Gets or sets the reference for the asynchronous method that would handle
        /// the Expanded event
        /// </summary>
        ExpandCollapseAsyncEventHandler OnExpanded { get; set; }

        /// <summary>
        /// Gets or sets the reference for the asychronous method that would handle
        /// the Collapsed event.
        /// </summary>
        ExpandCollapseEventHandler OnCollapsed { get; set; }

        /// <summary>
        /// Gets or sets the reference for the asychronous method that would handle
        /// the Selected event.
        /// </summary>
        SelectedItemAsyncEventHandler OnSelected { get; set; }

        /// <summary>
        /// Gets or sets the original Value handled by this item
        /// </summary>
        TItemValueType Value { get; }

        /// <summary>
        /// Gets or sets the parent node.
        /// </summary>
        ITreeViewNode<TItemValueType> Parent { get; }

        /// <summary>
        /// Gets or sets the reference to the owner TreeViewViewModel
        /// </summary>
        object OwnerTreeView { get; }

        /// <summary>
        /// Gets or sets the sub items
        /// </summary>
        IEnumerable<ITreeViewNode<TItemValueType>> Items { get; }

        /// <summary>
        /// Gets wether the Children are loaded.
        /// </summary>
        bool IsChildrenLoaded { get; }

        /// <summary>
        /// Gets wether this node is root.
        /// </summary>
        /// <remarks>
        /// If not parent has been defined, this node is considered as root.
        /// </remarks>
        bool IsRootNode { get; }

        /// <summary>
        /// Gets or sets wether this item should be treated as parentNode
        /// </summary>
        bool IsParentNode { get; set; }

        /// <summary>
        /// Gets wether this item is a leaf node.
        /// </summary>
        bool IsLeaf { get; }

        /// <summary>
        /// Gets or sets wether the item is in an Expanded state.
        /// </summary>
        bool IsExpanded { get; set; }

        /// <summary>
        /// Gets or sets wether the item is in Editing state.
        /// </summary>
        bool IsEditing { get; set; }

        /// <summary>
        /// Gets or sets wether the item can be edited.
        /// </summary>
        bool IsEditable { get; set; }

        /// <summary>
        /// Gets or sets wether the item is Selected
        /// </summary>
        bool IsSelected { get; set; }

        bool IsMultiSelected { get; set; }

        /// <summary>
        /// Gets wether this item is a proxy node, used as a placeholder for children items.
        /// </summary>
        bool IsProxyNode { get; }

        bool IsItemContainerNode { get; }

        /// <summary>
        /// Expands the item and all sub items.
        /// </summary>
        void ExpandAll(bool forceReload = false);

        /// <summary>
        /// Collapses the item and all sub items.
        /// </summary>
        void CollapseAll();

        /// <summary>
        /// Loads the children of all nodes without expanding the parents.
        /// </summary>
        void LoadAll();

        Task LoadAllAsync();

        /// <summary>
        /// Executes the loading of the node's children.
        /// </summary>
        /// <para>
        /// <param name="forceIfAlreadyLoaded">Indicates if the loading of children should be applied even if they were loaded previously.</param>
        /// </para>
        Task LoadChildren(bool forceIfAlreadyLoaded = false, bool deepLoadChildren = false, bool expandAfterLoad = false, bool maintainCurrentExpandStatus = true);

        /// <summary>
        /// Executes both Expand and Select on the node.
        /// </summary>
        void ExpandSelect();

        void EnsureVisibleSelected();

        ICommand EnterKeyCommand { get; }

        void RefreshData();
        void Refresh();
        bool IsBeingDragged { get; set; }

        void SetSilentSelect(bool isSelected);

        void SetSilentExpand(bool isExpanded);
        ICommand DoubleClickCommand { get; }
    }
}