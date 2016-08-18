using System.Collections.Generic;
using System.ComponentModel;

namespace WpfMultiselectTreeViewKit.Interfaces
{
    public interface ITreeView<out TItemValueType> : INotifyPropertyChanged
        where TItemValueType : ITreeViewNodeValue
    {
        /// <summary>
        /// Gets or sets wether the children of selected items should be loaded
        /// </summary>
        bool LoadChildrenOnSelected { get; set; }

        /// <summary>
        /// Gets wether nodes should be unloaded when an item is collapsed.
        /// </summary>
        bool ShouldUnloadChildrenOnCollapse { get; }

        /// <summary>
        /// Gets the collection of asynchronous tree view items used by the Tree View
        /// </summary>
        IEnumerable<ITreeViewNode<TItemValueType>> Items { get; }

        /// <summary>
        /// Gets the strongly typed collection of items
        /// </summary>
        IEnumerable<ITreeViewNode<TItemValueType>> SelectedItems { get; }

        /// <summary>
        /// Gets or sets the selected item
        /// </summary>
        ITreeViewNode<TItemValueType> LastSelectedItem { get; }

        /// <summary>
        /// Gets all of the currently loaded nodes
        /// </summary>
        IEnumerable<ITreeViewNode<TItemValueType>> GetAllNodes();
    }
}