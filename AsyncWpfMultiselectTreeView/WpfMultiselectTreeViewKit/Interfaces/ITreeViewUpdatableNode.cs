using System.Collections.Generic;
using System.ComponentModel;

namespace WpfMultiselectTreeViewKit.Interfaces
{
    public interface ITreeViewUpdatableNode<TItemValueType> where TItemValueType : ITreeViewNodeValue
    {
        /// <summary>
        /// Sets the value for the Items property
        /// </summary>
        /// <param name="items">The new collection.</param>
        void SetItems(IEnumerable<ITreeViewNode<TItemValueType>> items, bool resetCollection = true);

        /// <summary>
        /// Sets the value for the Items property
        /// </summary>
        /// <param name="item">The new collection.</param>
        void SetItems(ITreeViewNode<TItemValueType> item);

        /// <summary>
        /// Inserts a new item to the children collection.
        /// </summary>
        /// <param name="newChild">The new child.</param>
        void InsertChild(ITreeViewNode<TItemValueType> newChild);

        /// <summary>
        /// Removes an item from the childre collection.
        /// </summary>
        /// <param name="child">The item to remove.</param>
        void RemoveChild(ITreeViewNode<TItemValueType> child);
    }

}