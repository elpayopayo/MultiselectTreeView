namespace WpfMultiselectTreeViewKit.Interfaces
{
    /// <summary>
    /// Interface representing the View Model of a Tree View with an asynchronous loading behavior
    /// </summary>
    public interface IInitializableTreeView<in TDataSourceType>
    {
        /// <summary>
        /// Initializes the data for the tree view
        /// </summary>
        /// <param name="dataSource">The data that will be used to initialize the Items.</param>
        void Initialize(TDataSourceType dataSource);
    }
}