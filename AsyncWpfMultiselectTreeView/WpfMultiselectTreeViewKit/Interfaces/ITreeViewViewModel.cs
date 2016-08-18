namespace WpfMultiselectTreeViewKit.Interfaces
{
    public delegate void SelectedItemChangedEventHandler<in TItemValueType>(ITreeViewNode<TItemValueType> selectedItem)
        where TItemValueType : ITreeViewNodeValue;

    /// <summary>
    /// Interface representing the view model of a Tree View
    /// </summary>
    /// <typeparam name="TItemValueType">The type of object that is hosted by the nodes in this tree view.</typeparam>
    public interface ITreeViewViewModel<TItemValueType> 
        : ITreeView<TItemValueType>,
        INonTypedTreeViewPropertyHolder,
        IDragEnabledTreeView
        where TItemValueType : ITreeViewNodeValue
    {
        event SelectedItemChangedEventHandler<TItemValueType> SelectedItemChanged;

        /// <summary>
        /// Sets the last selected item.
        /// </summary>
        void SetLastSelectedItem(ITreeViewNode<TItemValueType> lastSelectedItem);
    }
}