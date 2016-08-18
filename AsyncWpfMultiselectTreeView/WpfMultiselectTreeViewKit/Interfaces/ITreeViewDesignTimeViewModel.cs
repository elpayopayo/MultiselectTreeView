namespace WpfMultiselectTreeViewKit.Interfaces
{
    /// <summary>
    /// Interface used only to aid in the declaration of a design-time view model for a control that would rely on a TreeViewViewModel DataContext
    /// </summary>
    interface ITreeViewDesignTimeViewModel : ITreeView<ITreeViewNodeValue>, INonTypedTreeViewPropertyHolder
    {
    }
}