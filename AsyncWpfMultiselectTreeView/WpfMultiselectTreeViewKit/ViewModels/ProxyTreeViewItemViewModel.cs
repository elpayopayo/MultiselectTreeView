using System;
using System.ComponentModel;
using WpfMultiselectTreeViewKit.Interfaces;

namespace WpfMultiselectTreeViewKit.ViewModels
{
    /// <summary>
    /// Represents the base view model for a tree view item that is considered as Proxy
    /// </summary>
    public abstract class ProxyTreeViewItemViewModel<TValue> : TreeViewItemViewModel<TValue> where TValue : ITreeViewNodeValue, IComparable
    {
        /// <summary>
        /// Gets wether the instance should be considered as Proxy.
        /// </summary>
        /// <remarks>Always returns true.</remarks>
        public override bool IsProxyNode
        {
            get { return true; }
        }

        public override bool IsParentNode
        {
            get { return false; }
        }

        public override bool IsLeaf
        {
            get { return false; }
        }

        /// <summary>
        /// Creates an instance of a ProxyTreeViewItemViewModel
        /// </summary>
        protected ProxyTreeViewItemViewModel(ITreeViewViewModel<TValue> ownerTree, ITreeViewNode<TValue> parentNode) : base(ownerTree, parentNode)
        {
        }

        public int CompareTo()
        {
            return 1;
        }

        protected override void OnEnteryKeyPressed()
        {
        }

        protected override void OnDoubleClick()
        {
        }
    }
}
