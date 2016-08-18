using System;
using System.ComponentModel;

namespace WpfMultiselectTreeViewKit.Interfaces
{
    public interface ITreeViewNodeValue : INotifyPropertyChanged
    {
        /// <summary>
        /// Gets the persistent content of this value
        /// </summary>
        IComparable PersistentContent { get; }
    }
}