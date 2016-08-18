using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;

namespace WpfMultiselectTreeViewKit.Utils
{
    public class SortedObservableTreeItemCollection<T> : ObservableCollection<T> where T : IComparable
    {
        #region Constructors

        public SortedObservableTreeItemCollection()
        {
        }

        public SortedObservableTreeItemCollection(IEnumerable<T> collection) : base(collection)
        {
        }

        public SortedObservableTreeItemCollection(List<T> list) : base(list)
        {
        }

        #endregion

        protected override void InsertItem(int index, T item)
        {
            var items = Items.ToList();
            items.Add(item);
            items.Sort((x,y)=>x.CompareTo(y));
            Items.Clear();
            Items.AddRange(items);
        }

        public void InitializeWith(IEnumerable<T> collection)
        {
            if (collection == null) throw new ArgumentNullException("collection");
            Items.Clear();
            AddRange(collection);
        }

        public void InitializeWith(T item)
        {
            if (item == null) throw new ArgumentNullException("item");
            Items.Clear();
            Add(item);
        }

        public void CombineWith(IEnumerable<T> collection)
        {
            bool areItemsInserted = false;
            bool areItemsRemoved = false;
            var currentItems = Items.ToList();
            var itemsList = collection.ToList();
            if (itemsList.Count < 100)
            {
                if (itemsList.Count > 500) { }
                var newItems = 
                    itemsList.Where(item => currentItems.All(currentItem => currentItem.CompareTo(item) != 0)).ToList();
                currentItems.AddRange(newItems);
                var insertedItems = newItems.Count;
                areItemsInserted = newItems.Any();
                if (insertedItems != itemsList.Count)
                {
                    var deletedItems = 
                        currentItems.Where(itemI => itemsList.All(itemJ => itemJ.CompareTo(itemI) != 0)).ToList();
                    foreach (var item in deletedItems)
                    {
                        currentItems.Remove(item);
                    }
                    areItemsRemoved = deletedItems.Any();
                }
            }
            //for performance
            else
            {
                areItemsInserted = true;
                currentItems.Clear();
                currentItems.AddRange(itemsList);
            }
            if(areItemsInserted || areItemsRemoved)
            {
                currentItems.Sort((x,y)=>x.CompareTo(y));
                Items.Clear();
                Items.AddRange(currentItems);
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            }
        }

        public long RemoveRange(IEnumerable<T> collection)
        {
            long removedItems = 0;
            if (collection == null) throw new ArgumentNullException("collection");

            var items = Items.ToList();

            foreach (var i in collection)
            {
                items.Remove(i);
                removedItems++;
            }
            items.Sort((x, y) => x.CompareTo(y));
            Items.Clear();
            Items.AddRange(items);
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            return removedItems;
        }

        public long AddRange(IEnumerable<T> collection)
        {
            if (collection == null) throw new ArgumentNullException("collection");
            
            var newItems = collection.ToList();
            newItems.Sort((x, y) => x.CompareTo(y));
            Items.AddRange(newItems);

            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            return newItems.Count;
        }

        
    }
}
