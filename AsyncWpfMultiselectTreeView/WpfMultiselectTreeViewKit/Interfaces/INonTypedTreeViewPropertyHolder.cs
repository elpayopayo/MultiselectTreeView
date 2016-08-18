using System.Collections;

namespace WpfMultiselectTreeViewKit.Interfaces
{
    public interface INonTypedTreeViewPropertyHolder
    {
        /// <summary>
        /// Gets or sets the non-typed collection of selected items
        /// </summary>
        /// <remarks>
        /// Used in the XAML TreeView control.
        /// </remarks>
        IList NonTypedSelectedItems { get; set; }

        /// <summary>
        /// Gets or sets the non-typed last selected item.
        /// </summary>
        /// <remarks>
        /// Used in the XAML TreeView control.
        /// </remarks>
        object NonTypedLastSelectedItem { get; set; }
    }
}