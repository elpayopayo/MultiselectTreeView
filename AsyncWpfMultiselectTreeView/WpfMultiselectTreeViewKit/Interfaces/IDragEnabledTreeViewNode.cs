using System.Windows;
using WpfMultiselectTreeViewKit.UserControl;

namespace WpfMultiselectTreeViewKit.Interfaces
{
    public interface IDragEnabledTreeView
    {
        void DoDragDrop(object dragSource);
        void DoDrop(DroppedData data);
        bool CanDrop(IDataObject source, object targetViewModel);
    }
}